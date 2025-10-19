/**
 * Enhanced HTTP Client with Trace Logging
 * 
 * Extends the base HttpClient with integrated trace logging for
 * comprehensive performance and behavior tracking.
 */

import HttpClient from './http-client.js';

class TracedHttpClient extends HttpClient {
  constructor(config, logger, traceLogger = null) {
    super(config, logger);
    this.traceLogger = traceLogger;
  }

  /**
   * Apply a RoomSpec to the operator with tracing
   */
  async applySpec(spec, options = {}) {
    const startTime = Date.now();
    const operation = 'applySpec';
    
    if (this.traceLogger) {
      this.traceLogger.logOperation(operation, {
        roomId: spec.room?.id,
        dryRun: options.dryRun || false,
        confirm: options.confirm || false,
      });
      this.traceLogger.logRequest('POST', '/apply', { spec, options });
    }
    
    const result = await super.applySpec(spec, options);
    const duration = Date.now() - startTime;
    
    if (this.traceLogger) {
      this.traceLogger.logResponse(
        'POST',
        '/apply',
        result.success ? 200 : (result.error?.status || 0),
        duration,
        result.success ? result.data : result.error
      );
      
      this.traceLogger.logMetric('apply_spec_duration', duration, 'ms');
      
      if (!result.success) {
        this.traceLogger.logError(operation, new Error(result.error?.message || 'Unknown error'), {
          spec,
          options,
        });
      }
    }
    
    return result;
  }

  /**
   * Get operator health with tracing
   */
  async getOperatorHealth() {
    const startTime = Date.now();
    const operation = 'getOperatorHealth';
    
    if (this.traceLogger) {
      this.traceLogger.logOperation(operation);
      this.traceLogger.logRequest('GET', '/health');
    }
    
    const result = await super.getOperatorHealth();
    const duration = Date.now() - startTime;
    
    if (this.traceLogger) {
      this.traceLogger.logResponse(
        'GET',
        '/health',
        result.success ? 200 : 0,
        duration
      );
      this.traceLogger.logMetric('health_check_duration', duration, 'ms');
    }
    
    return result;
  }

  /**
   * Check RoomServer health with tracing
   */
  async checkRoomServerHealth() {
    const startTime = Date.now();
    const operation = 'checkRoomServerHealth';
    
    if (this.traceLogger) {
      this.traceLogger.logOperation(operation);
      this.traceLogger.logRequest('GET', '/health');
    }
    
    const result = await super.checkRoomServerHealth();
    const duration = Date.now() - startTime;
    
    if (this.traceLogger) {
      this.traceLogger.logResponse(
        'GET',
        '/health',
        result.success ? 200 : 0,
        duration
      );
      this.traceLogger.logMetric('roomserver_health_check_duration', duration, 'ms');
    }
    
    return result;
  }

  /**
   * Get operator status with tracing
   */
  async getOperatorStatus() {
    const startTime = Date.now();
    const operation = 'getOperatorStatus';
    
    if (this.traceLogger) {
      this.traceLogger.logOperation(operation);
      this.traceLogger.logRequest('GET', '/status');
    }
    
    const result = await super.getOperatorStatus();
    const duration = Date.now() - startTime;
    
    if (this.traceLogger) {
      this.traceLogger.logResponse(
        'GET',
        '/status',
        result.success ? 200 : 0,
        duration
      );
      this.traceLogger.logMetric('get_status_duration', duration, 'ms');
    }
    
    return result;
  }

  /**
   * Get room status with tracing
   */
  async getRoomStatus(roomId) {
    const startTime = Date.now();
    const operation = 'getRoomStatus';
    
    if (this.traceLogger) {
      this.traceLogger.logOperation(operation, { roomId });
      this.traceLogger.logRequest('GET', `/status/rooms/${roomId}`);
    }
    
    const result = await super.getRoomStatus(roomId);
    const duration = Date.now() - startTime;
    
    if (this.traceLogger) {
      this.traceLogger.logResponse(
        'GET',
        `/status/rooms/${roomId}`,
        result.success ? 200 : 0,
        duration
      );
      this.traceLogger.logMetric('get_room_status_duration', duration, 'ms');
    }
    
    return result;
  }

  /**
   * Get room state with tracing
   */
  async getRoomState(roomId) {
    const startTime = Date.now();
    const operation = 'getRoomState';
    
    if (this.traceLogger) {
      this.traceLogger.logOperation(operation, { roomId });
      this.traceLogger.logRequest('GET', `/room/${roomId}/state`);
    }
    
    const result = await super.getRoomState(roomId);
    const duration = Date.now() - startTime;
    
    if (this.traceLogger) {
      this.traceLogger.logResponse(
        'GET',
        `/room/${roomId}/state`,
        result.success ? 200 : 0,
        duration
      );
      this.traceLogger.logMetric('get_room_state_duration', duration, 'ms');
    }
    
    return result;
  }

  /**
   * Log checkpoint in trace
   */
  logCheckpoint(name, details = {}) {
    if (this.traceLogger) {
      this.traceLogger.logCheckpoint(name, details);
    }
  }

  /**
   * Log assertion in trace
   */
  logAssertion(name, passed, expected = null, actual = null) {
    if (this.traceLogger) {
      this.traceLogger.logAssertion(name, passed, expected, actual);
    }
  }

  /**
   * Log custom metric
   */
  logMetric(metric, value, unit = null) {
    if (this.traceLogger) {
      this.traceLogger.logMetric(metric, value, unit);
    }
  }

  /**
   * Get trace summary
   */
  getTraceSummary() {
    if (this.traceLogger) {
      return this.traceLogger.getSummary();
    }
    return null;
  }

  /**
   * Flush trace logs
   */
  flushTrace() {
    if (this.traceLogger) {
      this.traceLogger.flush();
    }
  }
}

export default TracedHttpClient;
