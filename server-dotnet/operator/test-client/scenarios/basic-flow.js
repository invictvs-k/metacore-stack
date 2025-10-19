#!/usr/bin/env node

/**
 * Basic Flow Scenario - Happy Path Testing
 * 
 * Tests the complete lifecycle of RoomOperator reconciliation:
 * 1. Apply empty spec (create room)
 * 2. Add entities
 * 3. Seed artifacts
 * 4. Update policies
 * 5. Verify convergence
 * 6. Cleanup
 */

import { config, logger, HttpClient, MessageBuilder } from '../index.js';

class BasicFlowScenario {
  constructor() {
    this.client = new HttpClient(config, logger);
    this.testsPassed = 0;
    this.testsFailed = 0;
  }

  async run() {
    logger.section('Basic Flow Scenario - Happy Path');
    
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
      
      // Summary
      const success = logger.summary(this.testsPassed, this.testsFailed);
      process.exit(success ? 0 : 1);
      
    } catch (error) {
      logger.error('Scenario failed with unexpected error', error.message);
      logger.summary(this.testsPassed, this.testsFailed);
      process.exit(1);
    }
  }

  async step1_PreflightChecks() {
    logger.step(1, 'Preflight Checks');
    
    // Check operator health
    logger.info('Checking operator health...');
    const opHealth = await this.client.getOperatorHealth();
    
    if (opHealth.success) {
      logger.success('Operator is healthy', opHealth.data);
      this.testsPassed++;
    } else {
      logger.error('Operator health check failed', opHealth.error);
      this.testsFailed++;
      throw new Error('Operator not healthy');
    }
    
    // Check RoomServer health
    logger.info('Checking RoomServer health...');
    const rsHealth = await this.client.checkRoomServerHealth();
    
    if (rsHealth.success) {
      logger.success('RoomServer is healthy');
      this.testsPassed++;
    } else {
      logger.warn('RoomServer health check failed (might not have /health endpoint)', rsHealth.error);
      // Don't fail on this - RoomServer might not have health endpoint
    }
    
    await this.client.sleep(config.execution.delayBetweenOperations);
  }

  async step2_CreateEmptyRoom() {
    logger.step(2, 'Create Empty Room');
    
    const spec = MessageBuilder.buildMinimalSpec(config.testRoom.roomId);
    
    logger.info(`Creating room: ${config.testRoom.roomId}`);
    logger.debug('Spec:', spec);
    
    const result = await this.client.applySpec(spec);
    
    if (result.success) {
      logger.success('Room created successfully', result.data);
      this.testsPassed++;
      
      // Verify it's idempotent - apply again
      logger.info('Testing idempotency - applying same spec again...');
      const result2 = await this.client.applySpec(spec);
      
      if (result2.success) {
        logger.success('Idempotency verified - no errors on reapply');
        this.testsPassed++;
      } else {
        logger.error('Idempotency test failed', result2.error);
        this.testsFailed++;
      }
    } else {
      logger.error('Failed to create room', result.error);
      this.testsFailed++;
      throw new Error('Room creation failed');
    }
    
    await this.client.sleep(config.execution.delayBetweenOperations);
  }

  async step3_AddEntities() {
    logger.step(3, 'Add Entities');
    
    const spec = MessageBuilder.buildRoomSpec(config, {
      entities: config.entities,
      artifacts: [], // No artifacts yet
    });
    
    logger.info(`Adding ${config.entities.length} entities`);
    config.entities.forEach(e => {
      logger.debug(`  - ${e.id} (${e.kind})`);
    });
    
    const result = await this.client.applySpec(spec);
    
    if (result.success) {
      logger.success('Entities added successfully', {
        diff: result.data.diff,
        duration: result.data.duration,
      });
      
      // Verify entities were actually added
      if (result.data.diff && result.data.diff.toJoin) {
        const addedCount = result.data.diff.toJoin.length;
        logger.info(`Successfully joined ${addedCount} entities`);
        this.testsPassed++;
      } else {
        logger.warn('No entities joined in diff');
        this.testsFailed++;
      }
    } else {
      logger.error('Failed to add entities', result.error);
      this.testsFailed++;
      throw new Error('Entity addition failed');
    }
    
    await this.client.sleep(config.execution.delayBetweenOperations);
  }

  async step4_AddArtifacts() {
    logger.step(4, 'Add Artifacts');
    
    // For this test, we'll add artifacts by including them in the spec
    // In real usage, artifacts would have seedFrom pointing to files
    const spec = MessageBuilder.buildRoomSpec(config, {
      entities: config.entities,
      artifacts: config.artifacts.map(a => ({
        name: a.name,
        type: a.type,
        workspace: a.workspace,
        tags: a.tags,
      })),
    });
    
    logger.info(`Adding ${config.artifacts.length} artifacts`);
    config.artifacts.forEach(a => {
      logger.debug(`  - ${a.name} (${a.type})`);
    });
    
    logger.warn('Note: Artifacts require seedFrom files in real usage');
    logger.info('Applying spec with artifact definitions...');
    
    const result = await this.client.applySpec(spec);
    
    if (result.success) {
      logger.success('Spec with artifacts applied', {
        diff: result.data.diff,
        warnings: result.data.warnings,
      });
      
      // It's OK if artifacts weren't seeded (no files present)
      if (result.data.warnings && result.data.warnings.length > 0) {
        logger.info('Warnings (expected if seed files are missing):', result.data.warnings);
      }
      
      this.testsPassed++;
    } else {
      logger.error('Failed to apply spec with artifacts', result.error);
      this.testsFailed++;
      // Don't throw - artifact seeding might fail if files don't exist
    }
    
    await this.client.sleep(config.execution.delayBetweenOperations);
  }

  async step5_UpdatePolicies() {
    logger.step(5, 'Update Policies');
    
    const updatedPolicies = {
      ...config.policies,
      maxArtifactsPerEntity: 150, // Change a value
    };
    
    const spec = MessageBuilder.buildRoomSpec(config, {
      entities: config.entities,
      policies: updatedPolicies,
    });
    
    logger.info('Updating policies', updatedPolicies);
    
    const result = await this.client.applySpec(spec);
    
    if (result.success) {
      logger.success('Policies updated successfully', result.data);
      this.testsPassed++;
    } else {
      logger.error('Failed to update policies', result.error);
      this.testsFailed++;
    }
    
    await this.client.sleep(config.execution.delayBetweenOperations);
  }

  async step6_VerifyFinalState() {
    logger.step(6, 'Verify Final State');
    
    // Check operator status
    logger.info('Checking operator status...');
    const opStatus = await this.client.getOperatorStatus();
    
    if (opStatus.success) {
      logger.success('Operator status retrieved', {
        health: opStatus.data.health,
        rooms: opStatus.data.rooms?.length || 0,
      });
      this.testsPassed++;
      
      // Check if our room is there
      const ourRoom = opStatus.data.rooms?.find(r => r.roomId === config.testRoom.roomId);
      if (ourRoom) {
        logger.info('Our test room found in operator status', ourRoom);
        this.testsPassed++;
      } else {
        logger.warn('Test room not found in operator status');
      }
    } else {
      logger.error('Failed to get operator status', opStatus.error);
      this.testsFailed++;
    }
    
    // Check room state from RoomServer
    logger.info('Checking room state from RoomServer...');
    const roomState = await this.client.getRoomState(config.testRoom.roomId);
    
    if (roomState.success) {
      logger.success('Room state retrieved from RoomServer', {
        roomId: roomState.data.roomId,
        entitiesCount: roomState.data.entities?.length || 0,
        artifactsCount: roomState.data.artifacts?.length || 0,
      });
      
      // Verify entity count
      const expectedEntityCount = config.entities.length;
      const actualEntityCount = roomState.data.entities?.length || 0;
      
      if (actualEntityCount === expectedEntityCount) {
        logger.success(`Entity count matches: ${actualEntityCount}`);
        this.testsPassed++;
      } else {
        logger.warn(`Entity count mismatch: expected ${expectedEntityCount}, got ${actualEntityCount}`);
        this.testsFailed++;
      }
    } else {
      logger.error('Failed to get room state', roomState.error);
      this.testsFailed++;
    }
    
    await this.client.sleep(config.execution.delayBetweenOperations);
  }

  async step7_Cleanup() {
    logger.step(7, 'Cleanup');
    
    // Apply empty spec to remove all entities
    const spec = MessageBuilder.buildMinimalSpec(config.testRoom.roomId);
    
    logger.info('Removing all entities...');
    
    const result = await this.client.applySpec(spec);
    
    if (result.success) {
      logger.success('Cleanup successful', {
        diff: result.data.diff,
      });
      
      if (result.data.diff && result.data.diff.toKick) {
        logger.info(`Removed ${result.data.diff.toKick.length} entities`);
      }
      
      this.testsPassed++;
    } else {
      logger.error('Cleanup failed', result.error);
      this.testsFailed++;
    }
  }
}

// Run scenario if executed directly
if (import.meta.url === `file://${process.argv[1]}`) {
  const scenario = new BasicFlowScenario();
  scenario.run().catch(error => {
    logger.error('Unhandled error', error);
    process.exit(1);
  });
}

export default BasicFlowScenario;
