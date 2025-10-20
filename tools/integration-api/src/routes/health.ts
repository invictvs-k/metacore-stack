import { Router } from 'express';
import fetch from 'node-fetch';
import { getConfig } from '../services/config.js';

export const healthRouter = Router();

// Timeout for health check requests in milliseconds
const HEALTH_CHECK_TIMEOUT_MS = 5000;

// GET /api/health/roomserver - Check RoomServer health
healthRouter.get('/roomserver', async (req, res) => {
  try {
    const config = getConfig();
    const baseUrl = config.roomServer.baseUrl;
    
    let response;
    try {
      // Try /health endpoint first
      response = await fetch(`${baseUrl}/health`, {
        method: 'GET',
        signal: AbortSignal.timeout(HEALTH_CHECK_TIMEOUT_MS)
      });
    } catch {
      // Fallback to base URL
      try {
        response = await fetch(baseUrl, {
          method: 'GET',
          signal: AbortSignal.timeout(HEALTH_CHECK_TIMEOUT_MS)
        });
      } catch (error: any) {
        return res.status(503).json({
          status: 'error',
          service: 'roomserver',
          error: error.name === 'TimeoutError' ? 'Connection timeout' : (error.message || 'Connection failed'),
          endpoint: baseUrl
        });
      }
    }

    if (response.ok) {
      const data = await response.text();
      let parsed;
      try {
        parsed = JSON.parse(data);
      } catch {
        parsed = { status: 'ok', raw: data };
      }
      
      return res.json({
        status: 'healthy',
        service: 'roomserver',
        endpoint: baseUrl,
        data: parsed
      });
    } else {
      return res.status(response.status).json({
        status: 'unhealthy',
        service: 'roomserver',
        error: `HTTP ${response.status}`,
        endpoint: baseUrl
      });
    }
  } catch (error: any) {
    res.status(500).json({
      status: 'error',
      service: 'roomserver',
      error: error.message,
      action: 'Check that RoomServer is running and accessible'
    });
  }
});

// GET /api/health/roomoperator - Check RoomOperator health
healthRouter.get('/roomoperator', async (req, res) => {
  try {
    const config = getConfig();
    const baseUrl = config.roomOperator.baseUrl;
    
    let response;
    try {
      // Try /health endpoint first
      response = await fetch(`${baseUrl}/health`, {
        method: 'GET',
        signal: AbortSignal.timeout(HEALTH_CHECK_TIMEOUT_MS)
      });
    } catch {
      // Fallback to base URL
      try {
        response = await fetch(baseUrl, {
          method: 'GET',
          signal: AbortSignal.timeout(HEALTH_CHECK_TIMEOUT_MS)
        });
      } catch (error: any) {
        return res.status(503).json({
          status: 'error',
          service: 'roomoperator',
          error: error.name === 'TimeoutError' ? 'Connection timeout' : (error.message || 'Connection failed'),
          endpoint: baseUrl
        });
      }
    }

    if (response.ok) {
      const data = await response.text();
      let parsed;
      try {
        parsed = JSON.parse(data);
      } catch {
        parsed = { status: 'ok', raw: data };
      }
      
      return res.json({
        status: 'healthy',
        service: 'roomoperator',
        endpoint: baseUrl,
        data: parsed
      });
    } else {
      return res.status(response.status).json({
        status: 'unhealthy',
        service: 'roomoperator',
        error: `HTTP ${response.status}`,
        endpoint: baseUrl
      });
    }
  } catch (error: any) {
    res.status(500).json({
      status: 'error',
      service: 'roomoperator',
      error: error.message,
      action: 'Check that RoomOperator is running and accessible'
    });
  }
});

// GET /api/health/all - Check all services
healthRouter.get('/all', async (req, res) => {
  try {
    const config = getConfig();
    
    const results: any = {
      roomserver: { status: 'checking' },
      roomoperator: { status: 'checking' },
      mcp: { status: 'checking' }
    };

    // Check RoomServer
    try {
      const rsUrl = config.roomServer.baseUrl;
      let rsResponse;
      try {
        rsResponse = await fetch(`${rsUrl}/health`, {
          method: 'GET',
          signal: AbortSignal.timeout(HEALTH_CHECK_TIMEOUT_MS)
        });
      } catch {
        rsResponse = await fetch(rsUrl, {
          method: 'GET',
          signal: AbortSignal.timeout(HEALTH_CHECK_TIMEOUT_MS)
        });
      }
      
      results.roomserver = {
        status: rsResponse.ok ? 'healthy' : 'unhealthy',
        error: rsResponse.ok ? null : `HTTP ${rsResponse.status}`,
        endpoint: rsUrl
      };
    } catch (error: any) {
      results.roomserver = {
        status: 'error',
        error: error.name === 'TimeoutError' ? 'Connection timeout' : (error.message || 'Connection failed'),
        endpoint: config.roomServer.baseUrl
      };
    }

    // Check RoomOperator
    try {
      const roUrl = config.roomOperator.baseUrl;
      let roResponse;
      try {
        roResponse = await fetch(`${roUrl}/health`, {
          method: 'GET',
          signal: AbortSignal.timeout(HEALTH_CHECK_TIMEOUT_MS)
        });
      } catch {
        roResponse = await fetch(roUrl, {
          method: 'GET',
          signal: AbortSignal.timeout(HEALTH_CHECK_TIMEOUT_MS)
        });
      }
      
      results.roomoperator = {
        status: roResponse.ok ? 'healthy' : 'unhealthy',
        error: roResponse.ok ? null : `HTTP ${roResponse.status}`,
        endpoint: roUrl
      };
    } catch (error: any) {
      results.roomoperator = {
        status: 'error',
        error: error.name === 'TimeoutError' ? 'Connection timeout' : (error.message || 'Connection failed'),
        endpoint: config.roomOperator.baseUrl
      };
    }

    // Check MCP Status
    try {
      const mcpUrl = `${config.roomServer.baseUrl}/status/mcp`;
      const mcpResponse = await fetch(mcpUrl, {
        method: 'GET',
        signal: AbortSignal.timeout(HEALTH_CHECK_TIMEOUT_MS)
      });
      
      results.mcp = {
        status: mcpResponse.ok ? 'healthy' : 'unhealthy',
        error: mcpResponse.ok ? null : `HTTP ${mcpResponse.status}`,
        endpoint: mcpUrl
      };
    } catch (error: any) {
      results.mcp = {
        status: 'error',
        error: error.name === 'TimeoutError' ? 'Connection timeout' : (error.message || 'Connection failed'),
        endpoint: `${config.roomServer.baseUrl}/status/mcp`
      };
    }

    const allHealthy = Object.values(results).every((r: any) => r.status === 'healthy');
    
    res.json({
      overall: allHealthy ? 'healthy' : 'degraded',
      services: results,
      timestamp: new Date().toISOString()
    });
  } catch (error: any) {
    res.status(500).json({
      overall: 'error',
      error: error.message,
      timestamp: new Date().toISOString()
    });
  }
});
