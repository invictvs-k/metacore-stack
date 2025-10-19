/**
 * Trace Logger - NDJSON structured logging for integration test tracing
 * 
 * Outputs structured trace events in NDJSON format for analysis and replay.
 * Each event includes timestamps, operation details, and performance metrics.
 */

import fs from 'fs';
import path from 'path';

/**
 * TraceLogger provides structured event logging in NDJSON (Newline Delimited JSON) format
 * for comprehensive integration test tracing and performance analysis.
 * 
 * Features:
 * - NDJSON format output (one JSON object per line)
 * - Automatic timestamp and elapsed time tracking
 * - Multiple event types: operation, http_request, http_response, metric, assertion, checkpoint, error
 * - Automatic performance statistics calculation (P50/P95 latency, min/max/avg)
 * - In-memory event buffering with optional file output
 * 
 * Event Types:
 * - `operation`: High-level operation markers
 * - `http_request`: HTTP request sent
 * - `http_response`: HTTP response received (includes duration_ms)
 * - `metric`: Custom metric values
 * - `assertion`: Test assertion results (passed/failed)
 * - `checkpoint`: Test phase markers
 * - `error`: Error occurrences with stack traces
 * - `summary`: Final summary with statistics (auto-generated on flush)
 * 
 * @class TraceLogger
 * @example
 * // Create logger with file output
 * const logger = new TraceLogger('/path/to/trace.ndjson');
 * 
 * // Log various events
 * logger.logOperation('test_startup', { testId: 'test-1' });
 * logger.logRequest('GET', '/api/health');
 * logger.logResponse('GET', '/api/health', 200, 45);
 * logger.logAssertion('health_check', true, 'healthy', 'healthy');
 * logger.logMetric('response_time', 45, 'ms');
 * logger.logCheckpoint('test_complete');
 * 
 * // Get summary statistics
 * const summary = logger.getSummary();
 * console.log(`P50 latency: ${summary.latency.p50_ms}ms`);
 * 
 * // Flush and append summary to file
 * logger.flush();
 * 
 * @example
 * // Create logger without file output (in-memory only)
 * const logger = new TraceLogger();
 * logger.logOperation('test_operation');
 * const events = logger.getEvents();
 */
class TraceLogger {
  /**
   * Creates a new TraceLogger instance
   * 
   * @param {string|null} outputPath - Optional path to NDJSON output file.
   *                                   If provided, events are written to the file immediately.
   *                                   The directory will be created if it doesn't exist.
   *                                   If null, events are only stored in memory.
   * @constructor
   */
  constructor(outputPath = null) {
    this.outputPath = outputPath;
    this.events = [];
    this.startTime = Date.now();
    
    if (this.outputPath) {
      // Ensure directory exists
      const dir = path.dirname(this.outputPath);
      if (!fs.existsSync(dir)) {
        fs.mkdirSync(dir, { recursive: true });
      }
      
      // Create or truncate file
      fs.writeFileSync(this.outputPath, '');
    }
  }

  _createEvent(type, data = {}) {
    const now = Date.now();
    const event = {
      timestamp: new Date().toISOString(),
      timestamp_ms: now,
      elapsed_ms: now - this.startTime,
      type,
      ...data,
    };
    
    this.events.push(event);
    
    if (this.outputPath) {
      fs.appendFileSync(this.outputPath, JSON.stringify(event) + '\n');
    }
    
    return event;
  }

  logOperation(operation, details = {}) {
    return this._createEvent('operation', {
      operation,
      ...details,
    });
  }

  logRequest(method, url, data = null) {
    return this._createEvent('http_request', {
      method,
      url,
      data,
    });
  }

  logResponse(method, url, status, duration_ms, data = null) {
    return this._createEvent('http_response', {
      method,
      url,
      status,
      duration_ms,
      data,
    });
  }

  logError(operation, error, context = {}) {
    return this._createEvent('error', {
      operation,
      error: error.message || String(error),
      stack: error.stack,
      ...context,
    });
  }

  logMetric(metric, value, unit = null) {
    return this._createEvent('metric', {
      metric,
      value,
      unit,
    });
  }

  logCheckpoint(name, details = {}) {
    return this._createEvent('checkpoint', {
      name,
      ...details,
    });
  }

  logAssertion(name, passed, expected = null, actual = null) {
    return this._createEvent('assertion', {
      name,
      passed,
      expected,
      actual,
    });
  }

  getEvents() {
    return this.events;
  }

  getSummary() {
    const operations = this.events.filter(e => e.type === 'operation');
    const requests = this.events.filter(e => e.type === 'http_request');
    const responses = this.events.filter(e => e.type === 'http_response');
    const errors = this.events.filter(e => e.type === 'error');
    const assertions = this.events.filter(e => e.type === 'assertion');
    
    const durations = responses.map(r => r.duration_ms).filter(d => d != null);
    const sortedDurations = durations.sort((a, b) => a - b);
    
    const p50Index = Math.floor(sortedDurations.length * 0.5);
    const p95Index = Math.floor(sortedDurations.length * 0.95);
    
    return {
      total_events: this.events.length,
      operations: operations.length,
      http_requests: requests.length,
      http_responses: responses.length,
      errors: errors.length,
      assertions: {
        total: assertions.length,
        passed: assertions.filter(a => a.passed).length,
        failed: assertions.filter(a => !a.passed).length,
      },
      latency: {
        count: durations.length,
        min_ms: durations.length > 0 ? Math.min(...durations) : 0,
        max_ms: durations.length > 0 ? Math.max(...durations) : 0,
        avg_ms: durations.length > 0 ? durations.reduce((a, b) => a + b, 0) / durations.length : 0,
        p50_ms: sortedDurations[p50Index] || 0,
        p95_ms: sortedDurations[p95Index] || 0,
      },
      duration_ms: Date.now() - this.startTime,
    };
  }

  flush() {
    if (this.outputPath && this.events.length > 0) {
      const summary = this.getSummary();
      fs.appendFileSync(
        this.outputPath,
        JSON.stringify({ type: 'summary', ...summary }) + '\n'
      );
    }
  }
}

export default TraceLogger;
