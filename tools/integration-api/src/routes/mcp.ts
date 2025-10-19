import { Router } from 'express';
import { getConfig } from '../services/config.js';

export const mcpRouter = Router();

// GET /api/mcp/status - Proxy for RoomServer MCP status
mcpRouter.get('/status', async (req, res) => {
  try {
    const config = getConfig();
    const statusUrl = `${config.roomServer.baseUrl}/status/mcp`;

    // In a real implementation, we would fetch from RoomServer
    // For now, return mock data
    const mockStatus = {
      connected: false,
      providers: [],
      lastCheck: new Date().toISOString()
    };

    res.json(mockStatus);
  } catch (error: any) {
    res.status(500).json({ error: error.message });
  }
});
