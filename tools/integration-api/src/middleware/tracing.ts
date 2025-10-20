import type { Request, Response, NextFunction } from 'express';
import { randomUUID } from 'crypto';

declare global {
  namespace Express {
    interface Request {
      traceId?: string;
      runId?: string;
    }
  }
}

/**
 * Middleware to add traceId and runId to requests
 */
export function tracingMiddleware(req: Request, res: Response, next: NextFunction) {
  // Generate or use existing traceId
  req.traceId = req.headers['x-trace-id'] as string || randomUUID();
  
  // Generate or use existing runId
  req.runId = req.headers['x-run-id'] as string || randomUUID();
  
  // Add to response headers
  res.setHeader('X-Trace-Id', req.traceId);
  res.setHeader('X-Run-Id', req.runId);
  
  next();
}

/**
 * Enhanced logging with structured output
 */
export function structuredLogger(message: string, level: 'info' | 'warn' | 'error' = 'info', metadata?: any) {
  const logEntry = {
    timestamp: new Date().toISOString(),
    level,
    message,
    ...metadata
  };
  
  console.log(JSON.stringify(logEntry));
}
