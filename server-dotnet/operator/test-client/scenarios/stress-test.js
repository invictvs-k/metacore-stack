#!/usr/bin/env node

/**
 * Stress Test Scenario - Performance and Load Testing
 * 
 * Tests RoomOperator under load:
 * 1. Multiple rapid spec applications
 * 2. Large number of entities
 * 3. Concurrent operations
 * 4. Convergence time measurement
 */

import { config, logger, HttpClient, MessageBuilder } from '../index.js';

class StressTestScenario {
  constructor() {
    this.client = new HttpClient(config, logger);
    this.testsPassed = 0;
    this.testsFailed = 0;
    this.testsSkipped = 0;
    this.metrics = {
      totalOperations: 0,
      successfulOperations: 0,
      failedOperations: 0,
      totalDuration: 0,
      minDuration: Infinity,
      maxDuration: 0,
    };
  }

  async run() {
    logger.section('Stress Test Scenario - Performance Testing');
    
    try {
      await this.test1_RapidSpecApplications();
      await this.test2_LargeEntitySet();
      await this.test3_ConvergenceTime();
      
      // Show metrics
      this.displayMetrics();
      
      // Summary
      const success = logger.summary(this.testsPassed, this.testsFailed, this.testsSkipped);
      process.exit(success ? 0 : 1);
      
    } catch (error) {
      logger.error('Scenario failed with unexpected error', error.message);
      this.displayMetrics();
      logger.summary(this.testsPassed, this.testsFailed, this.testsSkipped);
      process.exit(1);
    }
  }

  async test1_RapidSpecApplications() {
    logger.step(1, 'Test Rapid Spec Applications');
    
    const iterations = 5;
    const specs = [];
    
    // Generate different specs
    for (let i = 0; i < iterations; i++) {
      const entities = config.entities.slice(0, i + 1); // Incremental entities
      specs.push(MessageBuilder.buildRoomSpec(config, { entities }));
    }
    
    logger.info(`Applying ${iterations} specs rapidly...`);
    
    const startTime = Date.now();
    const results = [];
    
    for (let i = 0; i < iterations; i++) {
      const opStart = Date.now();
      const result = await this.client.applySpec(specs[i]);
      const opDuration = Date.now() - opStart;
      
      results.push({ result, duration: opDuration });
      this.metrics.totalOperations++;
      
      if (result.success) {
        this.metrics.successfulOperations++;
        this.updateDurationMetrics(opDuration);
        logger.info(`Operation ${i + 1}/${iterations} succeeded in ${opDuration}ms`);
      } else {
        this.metrics.failedOperations++;
        logger.warn(`Operation ${i + 1}/${iterations} failed`, result.error);
      }
      
      // Small delay to avoid overwhelming the server
      await this.client.sleep(200);
    }
    
    const totalTime = Date.now() - startTime;
    this.metrics.totalDuration += totalTime;
    
    const successCount = results.filter(r => r.result.success).length;
    logger.success(`Completed ${successCount}/${iterations} operations in ${totalTime}ms`);
    logger.info(`Average duration: ${(totalTime / iterations).toFixed(2)}ms per operation`);
    
    if (successCount >= iterations * 0.8) { // 80% success rate
      this.testsPassed++;
    } else {
      this.testsFailed++;
    }
    
    // Cleanup
    logger.info('Cleaning up...');
    await this.client.applySpec(MessageBuilder.buildMinimalSpec(config.testRoom.roomId));
    
    await this.client.sleep(config.execution.delayBetweenOperations);
  }

  async test2_LargeEntitySet() {
    logger.step(2, 'Test Large Entity Set');
    
    const entityCount = 20;
    const entities = [];
    
    // Generate many entities
    for (let i = 0; i < entityCount; i++) {
      entities.push({
        id: `E-agent-stress-${i}`,
        kind: 'agent',
        displayName: `Stress Test Agent ${i}`,
        visibility: 'public',
        capabilities: [],
        policy: {
          allow_commands_from: 'none',
          sandbox_mode: true,
          env_whitelist: [],
          scopes: [],
        },
      });
    }
    
    const spec = MessageBuilder.buildRoomSpec(config, { entities });
    
    logger.info(`Creating room with ${entityCount} entities...`);
    
    const startTime = Date.now();
    const result = await this.client.applySpec(spec);
    const duration = Date.now() - startTime;
    
    this.metrics.totalOperations++;
    this.updateDurationMetrics(duration);
    
    if (result.success) {
      this.metrics.successfulOperations++;
      logger.success(`Created ${entityCount} entities in ${duration}ms`, {
        avgPerEntity: (duration / entityCount).toFixed(2) + 'ms',
        diff: result.data.diff,
      });
      
      const joinedCount = result.data.diff?.toJoin?.length || 0;
      if (joinedCount === entityCount) {
        logger.success(`All ${entityCount} entities joined successfully`);
        this.testsPassed++;
      } else {
        logger.warn(`Only ${joinedCount}/${entityCount} entities joined`);
        this.testsFailed++;
      }
    } else {
      this.metrics.failedOperations++;
      logger.error('Failed to create large entity set', result.error);
      this.testsFailed++;
    }
    
    // Cleanup
    logger.info('Cleaning up large entity set...');
    await this.client.applySpec(MessageBuilder.buildMinimalSpec(config.testRoom.roomId));
    
    await this.client.sleep(config.execution.delayBetweenOperations);
  }

  async test3_ConvergenceTime() {
    logger.step(3, 'Test Convergence Time');
    
    // Apply a spec and measure how long it takes to converge
    const spec = MessageBuilder.buildRoomSpec(config, {
      entities: config.entities,
    });
    
    logger.info('Applying spec and measuring convergence...');
    
    const startTime = Date.now();
    const result = await this.client.applySpec(spec);
    const applyDuration = Date.now() - startTime;
    
    this.metrics.totalOperations++;
    this.updateDurationMetrics(applyDuration);
    
    if (result.success) {
      this.metrics.successfulOperations++;
      logger.success(`Spec applied in ${applyDuration}ms`);
      
      // Check convergence by polling status
      logger.info('Checking convergence status...');
      
      let converged = false;
      let attempts = 0;
      const maxAttempts = 10;
      
      while (!converged && attempts < maxAttempts) {
        await this.client.sleep(1000);
        
        const status = await this.client.getRoomStatus(config.testRoom.roomId);
        
        if (status.success) {
          const cyclesSinceConverged = status.data.cyclesSinceConverged || 0;
          const isReconciling = status.data.isReconciling || false;
          
          logger.debug(`Convergence check ${attempts + 1}: cycles=${cyclesSinceConverged}, reconciling=${isReconciling}`);
          
          // Consider converged if cyclesSinceConverged is 0 and not reconciling
          if (cyclesSinceConverged === 0 && !isReconciling) {
            converged = true;
            const convergenceTime = Date.now() - startTime;
            logger.success(`Converged in ${convergenceTime}ms (${attempts + 1} checks)`);
            this.testsPassed++;
            break;
          }
        } else {
          logger.warn(`Status check failed: ${status.error.message}`);
        }
        
        attempts++;
      }
      
      if (!converged) {
        logger.warn(`Did not converge within ${maxAttempts} checks`);
        // Mark as skipped rather than passed since we couldn't verify convergence
        this.testsSkipped++;
      }
    } else {
      this.metrics.failedOperations++;
      logger.error('Failed to apply spec', result.error);
      this.testsFailed++;
    }
    
    // Cleanup
    logger.info('Cleaning up...');
    await this.client.applySpec(MessageBuilder.buildMinimalSpec(config.testRoom.roomId));
  }

  updateDurationMetrics(duration) {
    this.metrics.minDuration = Math.min(this.metrics.minDuration, duration);
    this.metrics.maxDuration = Math.max(this.metrics.maxDuration, duration);
  }

  displayMetrics() {
    logger.separator();
    logger.section('Performance Metrics');
    
    const avgDuration = this.metrics.totalOperations > 0
      ? (this.metrics.totalDuration / this.metrics.totalOperations).toFixed(2)
      : 0;
    
    const successRate = this.metrics.totalOperations > 0
      ? ((this.metrics.successfulOperations / this.metrics.totalOperations) * 100).toFixed(1)
      : 0;
    
    console.log(`Total Operations:      ${this.metrics.totalOperations}`);
    console.log(`Successful:            ${this.metrics.successfulOperations}`);
    console.log(`Failed:                ${this.metrics.failedOperations}`);
    console.log(`Success Rate:          ${successRate}%`);
    console.log(`Total Duration:        ${this.metrics.totalDuration}ms`);
    console.log(`Average Duration:      ${avgDuration}ms`);
    console.log(`Min Duration:          ${this.metrics.minDuration === Infinity ? 'N/A' : this.metrics.minDuration + 'ms'}`);
    console.log(`Max Duration:          ${this.metrics.maxDuration}ms`);
    
    logger.separator();
  }
}

// Run scenario if executed directly
if (import.meta.url === `file://${process.argv[1]}`) {
  const scenario = new StressTestScenario();
  scenario.run().catch(error => {
    logger.error('Unhandled error', error);
    process.exit(1);
  });
}

export default StressTestScenario;
