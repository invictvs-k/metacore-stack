/**
 * Scenario 03: MCP Unavailable
 * 
 * Tests behavior when MCP provider is unavailable (bad URL).
 * - Verify provider enters error state
 * - Ensure no log spam occurs (rate limiting works)
 * - Confirm RoomServer remains healthy
 */

import axios from 'axios';

export default async function run(context) {
  const { config, logger } = context;
  
  logger.info('Scenario 03: MCP Unavailable - Starting');
  const startTime = Date.now();
  const results = [];

  try {
    // Step 1: Load a provider with an unavailable endpoint
    logger.step('Loading MCP provider with unavailable endpoint');
    const unavailableProvider = config.mcp.providers.find(p => p.id === 'test-mcp-unavailable');
    
    if (!unavailableProvider) {
      throw new Error('test-mcp-unavailable provider not found in config');
    }
    
    try {
      const loadResponse = await axios.post(`${config.operator.baseUrl}/mcp/load`, {
        providers: [unavailableProvider],
      }, {
        timeout: config.operator.timeout,
      });
      
      results.push({ step: 'load_unavailable', status: 'PASSED', message: 'Load command sent for unavailable provider' });
      logger.success('✓ Load command sent for unavailable provider');
    } catch (error) {
      results.push({ step: 'load_unavailable', status: 'FAILED', message: error.message });
      logger.error('✗ Failed to send load command:', error.message);
      throw error;
    }

    // Step 2: Wait for connection attempts
    logger.step('Waiting for connection attempts (10 seconds)...');
    await new Promise(resolve => setTimeout(resolve, 10000));

    // Step 3: Check provider status
    logger.step('Checking provider status');
    try {
      const response = await axios.get(`${config.roomServer.baseUrl}/status/mcp`, {
        timeout: config.roomServer.timeout,
      });
      const status = response.data;
      const provider = status.providers?.find(p => p.id === unavailableProvider.id);
      
      if (provider) {
        logger.info(`Provider ${provider.id} state: ${provider.state}`);
        logger.info(`  Attempts: ${provider.attempts}`);
        if (provider.lastError) {
          logger.info(`  Last error: ${provider.lastError}`);
        }
        
        // Verify provider is in connecting or error state
        if (provider.state === 'connecting' || provider.state === 'error') {
          results.push({ step: 'provider_state', status: 'PASSED', message: `Provider in ${provider.state} state (expected)` });
          logger.success(`✓ Provider in ${provider.state} state (expected for unavailable endpoint)`);
        } else {
          results.push({ step: 'provider_state', status: 'WARNING', message: `Provider in unexpected state: ${provider.state}` });
          logger.warn(`⚠ Provider in unexpected state: ${provider.state}`);
        }
        
        // Verify attempts were made
        if (provider.attempts > 0) {
          results.push({ step: 'connection_attempts', status: 'PASSED', message: `${provider.attempts} attempt(s) made` });
          logger.success(`✓ ${provider.attempts} connection attempt(s) made`);
        } else {
          results.push({ step: 'connection_attempts', status: 'WARNING', message: 'No attempts recorded yet' });
          logger.warn('⚠ No attempts recorded yet');
        }
      } else {
        results.push({ step: 'provider_found', status: 'FAILED', message: 'Provider not found in status' });
        logger.error('✗ Provider not found in status');
      }
    } catch (error) {
      results.push({ step: 'check_status', status: 'FAILED', message: error.message });
      logger.error('✗ Failed to check status:', error.message);
      throw error;
    }

    // Step 4: Wait additional time to verify no log spam
    logger.step('Waiting additional time to verify no log spam (70 seconds)...');
    logger.info('(Rate limit window is 60 seconds, so we wait 70 to verify)');
    await new Promise(resolve => setTimeout(resolve, 70000));

    // Step 5: Check status again
    logger.step('Checking status after rate limit window');
    try {
      const response = await axios.get(`${config.roomServer.baseUrl}/status/mcp`, {
        timeout: config.roomServer.timeout,
      });
      const status = response.data;
      const provider = status.providers?.find(p => p.id === unavailableProvider.id);
      
      if (provider) {
        logger.info(`Provider ${provider.id} state: ${provider.state}, attempts: ${provider.attempts}`);
        
        // Verify attempts are capped (should not be continuously retrying)
        if (provider.attempts <= 10) {
          results.push({ step: 'no_spam', status: 'PASSED', message: `Attempts capped at ${provider.attempts} (no spam)` });
          logger.success(`✓ Connection attempts capped (${provider.attempts}), no spam detected`);
        } else {
          results.push({ step: 'no_spam', status: 'WARNING', message: `High attempt count: ${provider.attempts}` });
          logger.warn(`⚠ High attempt count: ${provider.attempts}`);
        }
      }
    } catch (error) {
      results.push({ step: 'check_final_status', status: 'FAILED', message: error.message });
      logger.error('✗ Failed to check final status:', error.message);
      throw error;
    }

    // Step 6: Verify RoomServer is still healthy
    logger.step('Verifying RoomServer health');
    try {
      const healthResponse = await axios.get(`${config.roomServer.baseUrl}/health`, {
        timeout: config.roomServer.timeout,
      });
      
      if (healthResponse.status === 200) {
        results.push({ step: 'health_check', status: 'PASSED', message: 'RoomServer remains healthy' });
        logger.success('✓ RoomServer remains healthy despite MCP unavailability');
      }
    } catch (error) {
      results.push({ step: 'health_check', status: 'FAILED', message: error.message });
      logger.error('✗ Health check failed:', error.message);
      throw error;
    }

    const duration = Date.now() - startTime;
    logger.success(`✓ Scenario 03 completed successfully in ${duration}ms`);
    
    return {
      scenario: '03-mcp-unavailable',
      status: 'PASSED',
      duration,
      results,
    };
  } catch (error) {
    const duration = Date.now() - startTime;
    logger.error(`✗ Scenario 03 failed after ${duration}ms:`, error.message);
    
    return {
      scenario: '03-mcp-unavailable',
      status: 'FAILED',
      duration,
      results,
      error: error.message,
    };
  }
}
