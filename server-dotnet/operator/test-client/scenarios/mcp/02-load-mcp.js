/**
 * Scenario 02: Load MCP
 * 
 * Tests loading MCP providers on-demand via RoomOperator command.
 * - Trigger MCP load via RoomOperator
 * - Verify providers attempt to connect
 * - Check status reflects connecting/connected/error states
 */

import axios from 'axios';

export default async function run(context) {
  const { config, logger } = context;
  
  logger.info('Scenario 02: Load MCP - Starting');
  const startTime = Date.now();
  const results = [];

  try {
    // Step 1: Get initial MCP status
    logger.step('Getting initial MCP status');
    let initialStatus;
    try {
      const response = await axios.get(`${config.roomServer.baseUrl}/status/mcp`, {
        timeout: config.roomServer.timeout,
      });
      initialStatus = response.data;
      const providerCount = initialStatus.providers?.length || 0;
      results.push({ step: 'initial_status', status: 'PASSED', message: `Initial providers: ${providerCount}` });
      logger.info(`Initial MCP status: ${providerCount} provider(s)`);
    } catch (error) {
      results.push({ step: 'initial_status', status: 'FAILED', message: error.message });
      logger.error('✗ Failed to get initial status:', error.message);
      throw error;
    }

    // Step 2: Send load command via RoomOperator
    logger.step('Sending MCP load command via RoomOperator');
    try {
      const loadResponse = await axios.post(`${config.operator.baseUrl}/mcp/load`, {
        providers: config.mcp.providers,
      }, {
        timeout: config.operator.timeout,
      });
      
      if (loadResponse.status === 200) {
        results.push({ step: 'load_command', status: 'PASSED', message: `Loaded ${config.mcp.providers.length} provider(s)` });
        logger.success(`✓ MCP load command sent successfully (${config.mcp.providers.length} providers)`);
      } else {
        results.push({ step: 'load_command', status: 'FAILED', message: `Unexpected status: ${loadResponse.status}` });
        logger.error('✗ MCP load command failed');
      }
    } catch (error) {
      results.push({ step: 'load_command', status: 'FAILED', message: error.message });
      logger.error('✗ MCP load command failed:', error.message);
      throw error;
    }

    // Step 3: Wait for providers to attempt connection
    logger.step('Waiting for providers to initiate connection...');
    await new Promise(resolve => setTimeout(resolve, 3000));

    // Step 4: Check MCP status after load
    logger.step('Checking MCP status after load');
    try {
      const response = await axios.get(`${config.roomServer.baseUrl}/status/mcp`, {
        timeout: config.roomServer.timeout,
      });
      const status = response.data;
      const providers = status.providers || [];
      
      if (providers.length > 0) {
        results.push({ step: 'status_after_load', status: 'PASSED', message: `${providers.length} provider(s) loaded` });
        logger.success(`✓ MCP status shows ${providers.length} provider(s)`);
        
        // Log each provider's state
        providers.forEach(provider => {
          logger.info(`  - ${provider.id}: ${provider.state} (attempts: ${provider.attempts})`);
          if (provider.lastError) {
            logger.warn(`    Last error: ${provider.lastError}`);
          }
        });
        
        // Verify at least one provider tried to connect
        const hasAttempts = providers.some(p => p.attempts > 0 || p.state !== 'idle');
        if (hasAttempts) {
          results.push({ step: 'connection_attempted', status: 'PASSED', message: 'At least one provider attempted connection' });
          logger.success('✓ Providers attempted to connect');
        } else {
          results.push({ step: 'connection_attempted', status: 'WARNING', message: 'No connection attempts detected yet' });
          logger.warn('⚠ No connection attempts detected yet');
        }
      } else {
        results.push({ step: 'status_after_load', status: 'FAILED', message: 'No providers found after load' });
        logger.error('✗ No providers found after load');
      }
    } catch (error) {
      results.push({ step: 'status_after_load', status: 'FAILED', message: error.message });
      logger.error('✗ Failed to get status after load:', error.message);
      throw error;
    }

    // Step 5: Verify RoomServer is still healthy
    logger.step('Verifying RoomServer health after MCP load');
    try {
      const healthResponse = await axios.get(`${config.roomServer.baseUrl}/health`, {
        timeout: config.roomServer.timeout,
      });
      
      if (healthResponse.status === 200) {
        results.push({ step: 'health_after_load', status: 'PASSED', message: 'RoomServer remains healthy' });
        logger.success('✓ RoomServer remains healthy after MCP load');
      }
    } catch (error) {
      results.push({ step: 'health_after_load', status: 'FAILED', message: error.message });
      logger.error('✗ RoomServer health check failed:', error.message);
      throw error;
    }

    const duration = Date.now() - startTime;
    logger.success(`✓ Scenario 02 completed successfully in ${duration}ms`);
    
    return {
      scenario: '02-load-mcp',
      status: 'PASSED',
      duration,
      results,
    };
  } catch (error) {
    const duration = Date.now() - startTime;
    logger.error(`✗ Scenario 02 failed after ${duration}ms:`, error.message);
    
    return {
      scenario: '02-load-mcp',
      status: 'FAILED',
      duration,
      results,
      error: error.message,
    };
  }
}
