import { Router } from 'express';
import type { Request, Response } from 'express';
import { Readable } from 'stream';
import fetch, { AbortError } from 'node-fetch';
import { getConfig } from '../services/config.js';

export const eventsRouter = Router();

// SSE Helper function
function setupSSE(res: Response) {
  const config = getConfig();
  const retryInterval = config.ui?.sseReconnectInterval || 5000;
  
  res.setHeader('Content-Type', 'text/event-stream');
  res.setHeader('Cache-Control', 'no-cache');
  res.setHeader('Connection', 'keep-alive');
  res.setHeader('X-Accel-Buffering', 'no');
  res.write(`retry: ${retryInterval}\n\n`);
  res.flushHeaders();
}

function sendSSEMessage(res: Response, data: any) {
  res.write(`data: ${JSON.stringify(data)}\n\n`);
}

function parseAndForwardChunk(
  chunk: string,
  res: Response,
  source: 'roomserver' | 'roomoperator'
) {
  const events = chunk.split(/\n\n/);

  for (const rawEvent of events) {
    if (!rawEvent.trim()) {
      continue;
    }

    const lines = rawEvent.split('\n');
    const dataLines: string[] = [];
    let eventType: string | undefined;
    let eventId: string | undefined;

    for (const line of lines) {
      if (line.startsWith('data:')) {
        dataLines.push(line.slice(5).trimStart());
      } else if (line.startsWith('event:')) {
        eventType = line.slice(6).trim();
      } else if (line.startsWith('id:')) {
        eventId = line.slice(3).trim();
      }
    }

    if (dataLines.length === 0) {
      continue;
    }

    const dataPayload = dataLines.join('\n');
    let parsed: any;

    try {
      parsed = JSON.parse(dataPayload);
    } catch {
      parsed = dataPayload;
    }

    const message: Record<string, any> =
      parsed && typeof parsed === 'object'
        ? { ...parsed }
        : {
          type: eventType ?? 'message',
          data: parsed
        };

    if (!message.type) {
      message.type = eventType ?? 'message';
    }

    message.source = message.source ?? source;
    message.timestamp = message.timestamp ?? new Date().toISOString();

    if (eventId) {
      message.id = eventId;
    }

    sendSSEMessage(res, message);
  }
}

async function proxyEventStream(
  _req: Request,
  res: Response,
  eventUrl: string,
  source: 'roomserver' | 'roomoperator'
) {
  const controller = new AbortController();
  let isConnected = false;

  try {
    const upstreamResponse = await fetch(eventUrl, {
      headers: { Accept: 'text/event-stream' },
      signal: controller.signal
    });

    if (!upstreamResponse.ok || !upstreamResponse.body) {
      // Only send error once when connection fails
      if (!isConnected) {
        sendSSEMessage(res, {
          source,
          type: 'error',
          timestamp: new Date().toISOString(),
          error: `Service unavailable (HTTP ${upstreamResponse.status})`
        });
      }
      return () => controller.abort();
    }

    isConnected = true;

    // Use the body as an async iterable
    (async () => {
      try {
        for await (const chunk of upstreamResponse.body as AsyncIterable<Buffer>) {
          parseAndForwardChunk(chunk.toString(), res, source);
        }
        // Connection ended normally
        if (isConnected) {
          sendSSEMessage(res, {
            source,
            type: 'disconnected',
            timestamp: new Date().toISOString(),
            message: 'Connection closed by server'
          });
        }
      } catch (error: any) {
        if (!controller.signal.aborted && isConnected) {
          sendSSEMessage(res, {
            source,
            type: 'error',
            timestamp: new Date().toISOString(),
            error: `Connection error: ${error.message}`
          });
        }
      }
    })();

    const cleanup = () => {
      controller.abort();
    };

    return cleanup;
  } catch (error: any) {
    // Only send connection error once
    if (!isConnected) {
      const message = error instanceof AbortError ? 'Connection aborted' : `Cannot connect: ${error.message}`;
      sendSSEMessage(res, {
        source,
        type: 'error',
        timestamp: new Date().toISOString(),
        error: message
      });
    }
    return () => controller.abort();
  }
}

// GET /api/events/roomserver - SSE proxy for RoomServer events
eventsRouter.get('/roomserver', async (req: Request, res: Response) => {
  setupSSE(res);

  const config = getConfig();
  const eventUrl = `${config.roomServer.baseUrl}${config.roomServer.events.path}`;

  // Heartbeat to keep connection alive
  const heartbeat = setInterval(() => {
    res.write(': ping\n\n');
  }, 10000);

  const cleanup = await proxyEventStream(req, res, eventUrl, 'roomserver');

  req.on('close', () => {
    clearInterval(heartbeat);
    cleanup?.();
  });
});

// GET /api/events/roomoperator - SSE proxy for RoomOperator events
eventsRouter.get('/roomoperator', async (req: Request, res: Response) => {
  setupSSE(res);

  const config = getConfig();
  const eventUrl = `${config.roomOperator.baseUrl}${config.roomOperator.events.path}`;

  // Heartbeat to keep connection alive
  const heartbeat = setInterval(() => {
    res.write(': ping\n\n');
  }, 10000);

  const cleanup = await proxyEventStream(req, res, eventUrl, 'roomoperator');

  req.on('close', () => {
    clearInterval(heartbeat);
    cleanup?.();
  });
});

// GET /api/events/combined - Combined SSE stream from both sources
eventsRouter.get('/combined', async (req: Request, res: Response) => {
  setupSSE(res);

  const config = getConfig();
  const roomServerUrl = `${config.roomServer.baseUrl}${config.roomServer.events.path}`;
  const roomOperatorUrl = `${config.roomOperator.baseUrl}${config.roomOperator.events.path}`;

  // Heartbeat to keep connection alive
  const heartbeat = setInterval(() => {
    res.write(': ping\n\n');
  }, 10000);

  const cleanupRoomServer = await proxyEventStream(req, res, roomServerUrl, 'roomserver');
  const cleanupRoomOperator = await proxyEventStream(req, res, roomOperatorUrl, 'roomoperator');

  req.on('close', () => {
    clearInterval(heartbeat);
    cleanupRoomServer?.();
    cleanupRoomOperator?.();
  });
});
