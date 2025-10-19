import { Router } from 'express';
import type { Request, Response } from 'express';
import { getConfig } from '../services/config.js';

export const eventsRouter = Router();

// SSE Helper function
function setupSSE(res: Response) {
  res.setHeader('Content-Type', 'text/event-stream');
  res.setHeader('Cache-Control', 'no-cache');
  res.setHeader('Connection', 'keep-alive');
  res.setHeader('X-Accel-Buffering', 'no');
  res.write('retry: 5000\n\n');
  res.flushHeaders();
}

function sendSSEMessage(res: Response, data: any) {
  res.write(`data: ${JSON.stringify(data)}\n\n`);
}

// GET /api/events/roomserver - SSE proxy for RoomServer events
eventsRouter.get('/roomserver', async (req: Request, res: Response) => {
  setupSSE(res);

  const config = getConfig();
  const eventUrl = `${config.roomServer.baseUrl}${config.roomServer.events.path}`;

  // Send initial connection message
  sendSSEMessage(res, {
    type: 'connected',
    source: 'roomserver',
    timestamp: new Date().toISOString()
  });

  // Heartbeat to keep connection alive
  const heartbeat = setInterval(() => {
    res.write(': ping\n\n');
  }, 10000);

  // Simulate connection to RoomServer (in production, this would be a real EventSource)
  // For now, we'll send periodic status updates
  const mockInterval = setInterval(() => {
    sendSSEMessage(res, {
      type: 'status',
      source: 'roomserver',
      timestamp: new Date().toISOString(),
      data: { status: 'running', connections: 1 }
    });
  }, 30000);

  req.on('close', () => {
    clearInterval(heartbeat);
    clearInterval(mockInterval);
  });
});

// GET /api/events/roomoperator - SSE proxy for RoomOperator events
eventsRouter.get('/roomoperator', async (req: Request, res: Response) => {
  setupSSE(res);

  const config = getConfig();
  const eventUrl = `${config.roomOperator.baseUrl}${config.roomOperator.events.path}`;

  // Send initial connection message
  sendSSEMessage(res, {
    type: 'connected',
    source: 'roomoperator',
    timestamp: new Date().toISOString()
  });

  // Heartbeat to keep connection alive
  const heartbeat = setInterval(() => {
    res.write(': ping\n\n');
  }, 10000);

  // Simulate connection to RoomOperator
  const mockInterval = setInterval(() => {
    sendSSEMessage(res, {
      type: 'status',
      source: 'roomoperator',
      timestamp: new Date().toISOString(),
      data: { status: 'running', activeReconciliations: 0 }
    });
  }, 30000);

  req.on('close', () => {
    clearInterval(heartbeat);
    clearInterval(mockInterval);
  });
});

// GET /api/events/combined - Combined SSE stream from both sources
eventsRouter.get('/combined', async (req: Request, res: Response) => {
  setupSSE(res);

  // Send initial connection message
  sendSSEMessage(res, {
    type: 'connected',
    source: 'combined',
    timestamp: new Date().toISOString()
  });

  // Heartbeat to keep connection alive
  const heartbeat = setInterval(() => {
    res.write(': ping\n\n');
  }, 10000);

  // Simulate combined events
  const mockInterval = setInterval(() => {
    const source = Math.random() > 0.5 ? 'roomserver' : 'roomoperator';
    sendSSEMessage(res, {
      type: 'status',
      source,
      timestamp: new Date().toISOString(),
      data: { status: 'running' }
    });
  }, 15000);

  req.on('close', () => {
    clearInterval(heartbeat);
    clearInterval(mockInterval);
  });
});
