#!/usr/bin/env node

/**
 * Enhanced Basic Flow Scenario with Trace Logging
 * 
 * Extended version of the basic-flow scenario that includes comprehensive
 * tracing and metrics collection for integration test analysis.
 */

import path from 'path';
import { config, logger } from '../index.js';
import MessageBuilder from '../utils/message-builder.js';
import TraceLogger from '../utils/trace-logger.js';
import TracedHttpClient from '../utils/traced-http-client.js';

class EnhancedBasicFlowScenario {
  constructor(artifactsDir = null) {
    // Setup trace logging if artifacts directory provided
    const traceLogPath = artifactsDir 
      ? path.join(artifactsDir, 'results', 'trace.ndjson')
      : null;
    
    this.traceLogger = new TraceLogger(traceLogPath);
    this.client = new TracedHttpClient(config, logger, this.traceLogger);
    this.testsPassed = 0;
    this.testsFailed = 0;
  }

  async run() {
    logger.section('Enhanced Basic Flow Scenario - Happy Path');
    this.traceLogger.logCheckpoint('scenario_start', { name: 'basic-flow' });
    
    try {
      // Pre-flight checks
      await this.step1_PreflightChecks();
      
      // Create empty room
      await this.step2_CreateEmptyRoom();
      
      // Add entities
      await this.step3_AddEntities();
      
      // Add artifacts
      await this.step4_AddArtifacts();
      
      // Update policies
      await this.step5_UpdatePolicies();
      
      // Verify final state
      await this.step6_VerifyFinalState();
      
      // Cleanup
      await this.step7_Cleanup();
      
      // Finalize trace
      this.traceLogger.logCheckpoint('scenario_complete', {
        tests_passed: this.testsPassed,
        tests_failed: this.testsFailed,
      });
      
      this.client.flushTrace();
      
      // Summary
      const success = logger.summary(this.testsPassed, this.testsFailed);
      
      // Print trace summary
      this.printTraceSummary();
      
      process.exit(success ? 0 : 1);
      
    } catch (error) {
      logger.error('Scenario failed with unexpected error', error.message);
      this.traceLogger.logError('scenario_execution', error);
      this.client.flushTrace();
      logger.summary(this.testsPassed, this.testsFailed);
      process.exit(1);
    }
  }

  async step1_PreflightChecks() {
    logger.step(1, 'Preflight Checks');
    this.traceLogger.logCheckpoint('step1_start', { step: 'preflight_checks' });
    
    // Check operator health
    logger.info('Checking operator health...');
    const opHealth = await this.client.getOperatorHealth();
    
    if (opHealth.success) {
      logger.success('Operator is healthy', opHealth.data);
      this.client.logAssertion('operator_health', true);
      this.testsPassed++;
    } else {
      logger.error('Operator health check failed', opHealth.error);
      this.client.logAssertion('operator_health', false);
      this.testsFailed++;
      throw new Error('Operator not healthy');
    }
    
    // Check RoomServer health
    logger.info('Checking RoomServer health...');
    const rsHealth = await this.client.checkRoomServerHealth();
    
    if (rsHealth.success) {
      logger.success('RoomServer is healthy');
      this.client.logAssertion('roomserver_health', true);
      this.testsPassed++;
    } else {
      logger.warn('RoomServer health check failed (might not have /health endpoint)', rsHealth.error);
      // Don't fail on this - RoomServer might not have health endpoint
    }
    
    await this.client.sleep(config.execution.delayBetweenOperations);
    this.traceLogger.logCheckpoint('step1_complete');
  }

  async step2_CreateEmptyRoom() {
    logger.step(2, 'Create Empty Room');
    this.traceLogger.logCheckpoint('step2_start', { step: 'create_empty_room' });
    
    const spec = MessageBuilder.buildMinimalSpec(config.testRoom.roomId);
    
    logger.info(`Creating room: ${config.testRoom.roomId}`);
    logger.debug('Spec:', spec);
    
    const result = await this.client.applySpec(spec);
    
    if (result.success) {
      logger.success('Room created successfully', result.data);
      this.client.logAssertion('room_creation', true);
      this.testsPassed++;
      
      // Test idempotency
      logger.info('Testing idempotency (reapply same spec)...');
      const idempotentResult = await this.client.applySpec(spec);
      
      if (idempotentResult.success) {
        logger.success('Idempotency verified');
        this.client.logAssertion('room_creation_idempotency', true);
        this.testsPassed++;
      } else {
        logger.warn('Idempotency check different result', {
          first: result.data,
          second: idempotentResult.data,
        });
        this.client.logAssertion('room_creation_idempotency', false);
      }
    } else {
      logger.error('Failed to create room', result.error);
      this.client.logAssertion('room_creation', false);
      this.testsFailed++;
      throw new Error('Room creation failed');
    }
    
    await this.client.sleep(config.execution.delayBetweenOperations);
    this.traceLogger.logCheckpoint('step2_complete');
  }

  async step3_AddEntities() {
    logger.step(3, 'Add Entities');
    this.traceLogger.logCheckpoint('step3_start', { step: 'add_entities' });
    
    // Add entities incrementally
    for (let i = 0; i < config.entities.length; i++) {
      const entities = config.entities.slice(0, i + 1);
      const spec = MessageBuilder.buildSpecWithEntities(config.testRoom.roomId, entities);
      
      logger.info(`Adding entity ${i + 1}/${config.entities.length}: ${entities[i].id}`);
      
      const result = await this.client.applySpec(spec);
      
      if (result.success) {
        logger.success(`Entity ${entities[i].id} added`);
        this.client.logAssertion(`entity_add_${i}`, true);
        this.testsPassed++;
      } else {
        logger.error(`Failed to add entity ${entities[i].id}`, result.error);
        this.client.logAssertion(`entity_add_${i}`, false);
        this.testsFailed++;
      }
      
      await this.client.sleep(config.execution.delayBetweenOperations);
    }
    
    this.traceLogger.logCheckpoint('step3_complete');
  }

  async step4_AddArtifacts() {
    logger.step(4, 'Add Artifacts');
    this.traceLogger.logCheckpoint('step4_start', { step: 'add_artifacts' });
    
    const spec = MessageBuilder.buildSpecWithArtifacts(
      config.testRoom.roomId,
      config.entities,
      config.artifacts
    );
    
    logger.info(`Adding ${config.artifacts.length} artifacts...`);
    logger.debug('Spec:', spec);
    
    const result = await this.client.applySpec(spec);
    
    if (result.success) {
      logger.success('Artifacts added successfully', result.data);
      this.client.logAssertion('artifacts_add', true);
      this.testsPassed++;
    } else {
      logger.error('Failed to add artifacts', result.error);
      this.client.logAssertion('artifacts_add', false);
      this.testsFailed++;
    }
    
    await this.client.sleep(config.execution.delayBetweenOperations);
    this.traceLogger.logCheckpoint('step4_complete');
  }

  async step5_UpdatePolicies() {
    logger.step(5, 'Update Policies');
    this.traceLogger.logCheckpoint('step5_start', { step: 'update_policies' });
    
    const spec = MessageBuilder.buildFullSpec(
      config.testRoom.roomId,
      config.entities,
      config.artifacts,
      config.policies
    );
    
    logger.info('Updating policies...');
    logger.debug('Policies:', config.policies);
    
    const result = await this.client.applySpec(spec);
    
    if (result.success) {
      logger.success('Policies updated successfully', result.data);
      this.client.logAssertion('policies_update', true);
      this.testsPassed++;
    } else {
      logger.error('Failed to update policies', result.error);
      this.client.logAssertion('policies_update', false);
      this.testsFailed++;
    }
    
    await this.client.sleep(config.execution.delayBetweenOperations);
    this.traceLogger.logCheckpoint('step5_complete');
  }

  async step6_VerifyFinalState() {
    logger.step(6, 'Verify Final State');
    this.traceLogger.logCheckpoint('step6_start', { step: 'verify_final_state' });
    
    logger.info('Getting operator status...');
    const status = await this.client.getOperatorStatus();
    
    if (status.success) {
      logger.success('Operator status retrieved', status.data);
      this.client.logAssertion('get_operator_status', true);
      this.testsPassed++;
      
      // Verify room is tracked
      if (status.data.rooms && status.data.rooms.length > 0) {
        logger.success(`Found ${status.data.rooms.length} tracked room(s)`);
        this.client.logAssertion('rooms_tracked', true, 1, status.data.rooms.length);
        this.testsPassed++;
      } else {
        logger.warn('No rooms found in operator status');
        this.client.logAssertion('rooms_tracked', false, 1, 0);
      }
    } else {
      logger.error('Failed to get operator status', status.error);
      this.client.logAssertion('get_operator_status', false);
      this.testsFailed++;
    }
    
    await this.client.sleep(config.execution.delayBetweenOperations);
    this.traceLogger.logCheckpoint('step6_complete');
  }

  async step7_Cleanup() {
    logger.step(7, 'Cleanup');
    this.traceLogger.logCheckpoint('step7_start', { step: 'cleanup' });
    
    // Apply empty spec to remove all entities and artifacts
    const spec = MessageBuilder.buildMinimalSpec(config.testRoom.roomId);
    
    logger.info('Cleaning up room...');
    
    const result = await this.client.applySpec(spec);
    
    if (result.success) {
      logger.success('Room cleaned up successfully');
      this.client.logAssertion('cleanup', true);
      this.testsPassed++;
    } else {
      logger.warn('Cleanup had issues', result.error);
      this.client.logAssertion('cleanup', false);
      // Don't fail the test on cleanup issues
    }
    
    this.traceLogger.logCheckpoint('step7_complete');
  }

  printTraceSummary() {
    const summary = this.client.getTraceSummary();
    
    if (!summary) return;
    
    logger.separator();
    logger.section('Performance Metrics');
    
    console.log('  Total Duration:       ' + summary.duration_ms + 'ms');
    console.log('  HTTP Requests:        ' + summary.http_requests);
    console.log('  HTTP Responses:       ' + summary.http_responses);
    console.log('  Errors:               ' + summary.errors);
    console.log('');
    console.log('  Latency Statistics:');
    console.log('    Count:              ' + summary.latency.count);
    console.log('    Min:                ' + summary.latency.min_ms.toFixed(2) + 'ms');
    console.log('    Max:                ' + summary.latency.max_ms.toFixed(2) + 'ms');
    console.log('    Average:            ' + summary.latency.avg_ms.toFixed(2) + 'ms');
    console.log('    P50:                ' + summary.latency.p50_ms.toFixed(2) + 'ms');
    console.log('    P95:                ' + summary.latency.p95_ms.toFixed(2) + 'ms');
    console.log('');
  }
}

// Run if executed directly
if (import.meta.url === `file://${process.argv[1]}`) {
  const artifactsDir = process.env.ARTIFACTS_DIR || null;
  const scenario = new EnhancedBasicFlowScenario(artifactsDir);
  scenario.run();
}

export default EnhancedBasicFlowScenario;
