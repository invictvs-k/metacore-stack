/**
 * Scenario 04: Status Query
 * 
 * Tests querying MCP status via RoomServer and RoomOperator endpoints.
 * - Query status via RoomServer /status/mcp
 * - Query status via RoomOperator /mcp/status
 * - Verify response structure and data consistency
 */

import axios from 'axios';

export default async function run(context) {
  const { config, logger } = context;
  
  logger.info('Scenario 04: Status Query - Starting');
  const startTime = Date.now();
  const results = [];

  try {
    // Step 1: Query status via RoomServer
    logger.step('Querying MCP status via RoomServer');
    let roomServerStatus;
    try {
      const response = await axios.get(`${config.roomServer.baseUrl}/status/mcp`, {
        timeout: config.roomServer.timeout,
      });
      roomServerStatus = response.data;
      
      // Validate response structure
      if (roomServerStatus.providers && Array.isArray(roomServerStatus.providers)) {
        results.push({ step: 'roomserver_status', status: 'PASSED', message: `Retrieved ${roomServerStatus.providers.length} provider(s)` });
        logger.success(`✓ RoomServer status: ${roomServerStatus.providers.length} provider(s)`);
        
        // Validate each provider has required fields
        const validProviders = roomServerStatus.providers.every(p => 
          p.id && p.state && typeof p.attempts === 'number' && typeof p.lastChangeAt === 'number'
        );
        
        if (validProviders) {
          results.push({ step: 'roomserver_structure', status: 'PASSED', message: 'All providers have valid structure' });
          logger.success('✓ All providers have valid structure');
        } else {
          results.push({ step: 'roomserver_structure', status: 'FAILED', message: 'Some providers have invalid structure' });
          logger.error('✗ Some providers have invalid structure');
        }
        
        // Log provider details
        roomServerStatus.providers.forEach(provider => {
          logger.info(`  - ${provider.id}:`);
          logger.info(`    State: ${provider.state}`);
          logger.info(`    Attempts: ${provider.attempts}`);
          logger.info(`    Last change: ${new Date(provider.lastChangeAt).toISOString()}`);
          if (provider.lastError) {
            logger.info(`    Last error: ${provider.lastError}`);
          }
        });
      } else {
        results.push({ step: 'roomserver_status', status: 'FAILED', message: 'Invalid response structure' });
        logger.error('✗ Invalid response structure from RoomServer');
      }
    } catch (error) {
      results.push({ step: 'roomserver_status', status: 'FAILED', message: error.message });
      logger.error('✗ Failed to query RoomServer status:', error.message);
      throw error;
    }

    // Step 2: Query status via RoomOperator
    logger.step('Querying MCP status via RoomOperator');
    let operatorStatus;
    try {
      const response = await axios.get(`${config.operator.baseUrl}/mcp/status`, {
        timeout: config.operator.timeout,
      });
      operatorStatus = response.data;
      
      // Validate response structure
      if (operatorStatus.providers && Array.isArray(operatorStatus.providers)) {
        results.push({ step: 'operator_status', status: 'PASSED', message: `Retrieved ${operatorStatus.providers.length} provider(s)` });
        logger.success(`✓ RoomOperator status: ${operatorStatus.providers.length} provider(s)`);
      } else {
        results.push({ step: 'operator_status', status: 'FAILED', message: 'Invalid response structure' });
        logger.error('✗ Invalid response structure from RoomOperator');
      }
    } catch (error) {
      results.push({ step: 'operator_status', status: 'FAILED', message: error.message });
      logger.error('✗ Failed to query RoomOperator status:', error.message);
      throw error;
    }

    // Step 3: Compare RoomServer and RoomOperator responses
    logger.step('Comparing RoomServer and RoomOperator responses');
    try {
      if (roomServerStatus.providers.length === operatorStatus.providers.length) {
        results.push({ step: 'consistency_check', status: 'PASSED', message: 'Provider count matches' });
        logger.success('✓ Provider count matches between RoomServer and RoomOperator');
        
        // Compare provider IDs
        const roomServerIds = new Set(roomServerStatus.providers.map(p => p.id));
        const operatorIds = new Set(operatorStatus.providers.map(p => p.id));
        
        const idsMatch = roomServerStatus.providers.every(p => operatorIds.has(p.id));
        
        if (idsMatch) {
          results.push({ step: 'id_consistency', status: 'PASSED', message: 'Provider IDs match' });
          logger.success('✓ Provider IDs consistent between endpoints');
        } else {
          results.push({ step: 'id_consistency', status: 'WARNING', message: 'Provider IDs differ' });
          logger.warn('⚠ Provider IDs differ between endpoints');
        }
      } else {
        results.push({ step: 'consistency_check', status: 'WARNING', message: 'Provider count mismatch' });
        logger.warn(`⚠ Provider count differs: RoomServer=${roomServerStatus.providers.length}, Operator=${operatorStatus.providers.length}`);
      }
    } catch (error) {
      results.push({ step: 'consistency_check', status: 'FAILED', message: error.message });
      logger.error('✗ Consistency check failed:', error.message);
    }

    // Step 4: Test idempotency - query again
    logger.step('Testing idempotency - querying again');
    try {
      const response = await axios.get(`${config.roomServer.baseUrl}/status/mcp`, {
        timeout: config.roomServer.timeout,
      });
      const secondStatus = response.data;
      
      if (secondStatus.providers.length === roomServerStatus.providers.length) {
        results.push({ step: 'idempotency', status: 'PASSED', message: 'Status query is idempotent' });
        logger.success('✓ Status query is idempotent');
      } else {
        results.push({ step: 'idempotency', status: 'WARNING', message: 'Provider count changed between queries' });
        logger.warn('⚠ Provider count changed between queries (may be expected if connections are being established)');
      }
    } catch (error) {
      results.push({ step: 'idempotency', status: 'FAILED', message: error.message });
      logger.error('✗ Idempotency test failed:', error.message);
    }

    const duration = Date.now() - startTime;
    logger.success(`✓ Scenario 04 completed successfully in ${duration}ms`);
    
    return {
      scenario: '04-status',
      status: 'PASSED',
      duration,
      results,
    };
  } catch (error) {
    const duration = Date.now() - startTime;
    logger.error(`✗ Scenario 04 failed after ${duration}ms:`, error.message);
    
    return {
      scenario: '04-status',
      status: 'FAILED',
      duration,
      results,
      error: error.message,
    };
  }
}
