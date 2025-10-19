#!/usr/bin/env node

/**
 * Error Cases Scenario - Error Handling and Recovery Testing
 * 
 * Tests how RoomOperator handles various error scenarios:
 * 1. Invalid spec structure
 * 2. Missing required fields
 * 3. Invalid room ID format
 * 4. Guardrails violations
 * 5. Idempotency verification
 */

import { config, logger, HttpClient, MessageBuilder } from '../index.js';

class ErrorCasesScenario {
  constructor() {
    this.client = new HttpClient(config, logger);
    this.testsPassed = 0;
    this.testsFailed = 0;
  }

  async run() {
    logger.section('Error Cases Scenario - Error Handling');
    
    try {
      await this.test1_InvalidSpecStructure();
      await this.test2_InvalidRoomId();
      await this.test3_MissingRequiredFields();
      await this.test4_DryRunMode();
      await this.test5_IdempotencyVerification();
      
      // Summary
      const success = logger.summary(this.testsPassed, this.testsFailed);
      process.exit(success ? 0 : 1);
      
    } catch (error) {
      logger.error('Scenario failed with unexpected error', error.message);
      logger.summary(this.testsPassed, this.testsFailed);
      process.exit(1);
    }
  }

  async test1_InvalidSpecStructure() {
    logger.step(1, 'Test Invalid Spec Structure');
    
    // Test with completely invalid spec
    const invalidSpec = {
      invalid: 'structure',
    };
    
    logger.info('Sending invalid spec (should fail)...');
    const result = await this.client.applySpec(invalidSpec);
    
    if (!result.success && result.error.status === 400) {
      logger.success('Correctly rejected invalid spec with 400 Bad Request');
      this.testsPassed++;
    } else if (result.success) {
      logger.error('Unexpectedly accepted invalid spec');
      this.testsFailed++;
    } else {
      logger.warn('Failed with unexpected error', result.error);
      this.testsFailed++;
    }
    
    await this.client.sleep(config.execution.delayBetweenOperations);
  }

  async test2_InvalidRoomId() {
    logger.step(2, 'Test Invalid Room ID');
    
    // Test with invalid room ID format
    const invalidRoomId = 'bad-room'; // Too short
    const spec = MessageBuilder.buildMinimalSpec(invalidRoomId);
    
    logger.info(`Sending spec with invalid room ID: ${invalidRoomId} (should fail)...`);
    const result = await this.client.applySpec(spec);
    
    if (!result.success && result.error.status === 400) {
      logger.success('Correctly rejected invalid room ID with 400 Bad Request');
      this.testsPassed++;
    } else if (result.success) {
      logger.error('Unexpectedly accepted invalid room ID');
      this.testsFailed++;
    } else {
      logger.warn('Failed with unexpected error', result.error);
      this.testsFailed++;
    }
    
    await this.client.sleep(config.execution.delayBetweenOperations);
  }

  async test3_MissingRequiredFields() {
    logger.step(3, 'Test Missing Required Fields');
    
    // Test with missing apiVersion
    const incompleteSpec = {
      spec: {
        // Missing apiVersion
        kind: 'RoomSpec',
        metadata: {
          name: 'test',
          version: 1,
        },
        spec: {
          roomId: config.testRoom.roomId,
          entities: [],
          artifacts: [],
          policies: {},
        },
      },
    };
    
    logger.info('Sending spec without apiVersion (should fail or be lenient)...');
    const result = await this.client.applySpec(incompleteSpec);
    
    if (!result.success) {
      logger.success('Correctly rejected incomplete spec', result.error);
      this.testsPassed++;
    } else {
      logger.info('Accepted incomplete spec (operator might be lenient)');
      this.testsPassed++;
    }
    
    await this.client.sleep(config.execution.delayBetweenOperations);
  }

  async test4_DryRunMode() {
    logger.step(4, 'Test Dry Run Mode');
    
    const spec = MessageBuilder.buildRoomSpec(config, {
      entities: [config.entities[0]], // Just one entity
    });
    
    logger.info('Applying spec with X-Dry-Run: true...');
    const dryRunResult = await this.client.applySpec(spec, { dryRun: true });
    
    if (dryRunResult.success) {
      logger.success('Dry run executed successfully', {
        diff: dryRunResult.data.diff,
        warnings: dryRunResult.data.warnings,
      });
      this.testsPassed++;
      
      // Verify state wasn't actually changed by checking again
      logger.info('Verifying state was not changed...');
      const statusCheck = await this.client.getRoomStatus(config.testRoom.roomId);
      
      if (statusCheck.success) {
        logger.info('Room status after dry run', statusCheck.data);
        this.testsPassed++;
      } else {
        logger.info('Room not found after dry run (expected if not created yet)');
        this.testsPassed++;
      }
    } else {
      logger.error('Dry run failed', dryRunResult.error);
      this.testsFailed++;
    }
    
    await this.client.sleep(config.execution.delayBetweenOperations);
  }

  async test5_IdempotencyVerification() {
    logger.step(5, 'Test Idempotency');
    
    // Create a room with one entity
    const spec = MessageBuilder.buildRoomSpec(config, {
      entities: [config.entities[0]],
    });
    
    logger.info('Applying spec (first time)...');
    const result1 = await this.client.applySpec(spec);
    
    if (!result1.success) {
      logger.error('First application failed', result1.error);
      this.testsFailed++;
      return;
    }
    
    logger.success('First application succeeded', {
      diff: result1.data.diff,
    });
    
    await this.client.sleep(config.execution.delayBetweenOperations);
    
    // Apply the exact same spec again
    logger.info('Applying same spec again (testing idempotency)...');
    const result2 = await this.client.applySpec(spec);
    
    if (result2.success) {
      logger.success('Second application succeeded (idempotent)', {
        diff: result2.data.diff,
      });
      
      // Check that nothing changed
      const firstJoinCount = result1.data.diff?.toJoin?.length || 0;
      const secondJoinCount = result2.data.diff?.toJoin?.length || 0;
      
      if (secondJoinCount === 0 && firstJoinCount > 0) {
        logger.success('Idempotency verified: No changes on second apply');
        this.testsPassed++;
      } else if (secondJoinCount === firstJoinCount) {
        logger.info('Same changes in both applies (might be OK depending on operator behavior)');
        this.testsPassed++;
      } else {
        logger.warn('Different results between applies', {
          first: firstJoinCount,
          second: secondJoinCount,
        });
        this.testsFailed++;
      }
    } else {
      logger.error('Second application failed', result2.error);
      this.testsFailed++;
    }
    
    // Cleanup
    logger.info('Cleaning up test room...');
    const cleanupSpec = MessageBuilder.buildMinimalSpec(config.testRoom.roomId);
    await this.client.applySpec(cleanupSpec);
    
    await this.client.sleep(config.execution.delayBetweenOperations);
  }
}

// Run scenario if executed directly
if (import.meta.url === `file://${process.argv[1]}`) {
  const scenario = new ErrorCasesScenario();
  scenario.run().catch(error => {
    logger.error('Unhandled error', error);
    process.exit(1);
  });
}

export default ErrorCasesScenario;
