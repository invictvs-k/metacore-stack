import { Router } from 'express';
import fetch from 'node-fetch';
import { getConfig } from '../services/config.js';

export const mcpRouter = Router();

// GET /api/mcp/status - Proxy for RoomServer MCP status
mcpRouter.get('/status', async (req, res) => {
  try {
    const config = getConfig();
    const statusUrl = `${config.roomServer.baseUrl}/status/mcp`;

    const response = await fetch(statusUrl, {
      headers: {
        'Accept': 'application/json'
      }
    });

    if (!response.ok) {
      return res.status(response.status).json({
        error: `Failed to fetch MCP status from RoomServer (${response.status})`,
        action: 'Check that RoomServer is running and accessible',
        endpoint: statusUrl
      });
    }

    const data = await response.json();
    res.json(data);
  } catch (error: any) {
    res.status(500).json({ 
      error: error.message,
      action: 'Verify that RoomServer is running and the baseUrl in config is correct',
      endpoint: `${getConfig().roomServer.baseUrl}/status/mcp`
    });
  }
});
