import express from 'express';
import cors from 'cors';
import helmet from 'helmet';
import morgan from 'morgan';
import path from 'path';
import { fileURLToPath } from 'url';
import { configRouter } from './routes/config.js';
import { eventsRouter } from './routes/events.js';
import { testsRouter } from './routes/tests.js';
import { commandsRouter } from './routes/commands.js';
import { mcpRouter } from './routes/mcp.js';
import { healthRouter } from './routes/health.js';
import { loadConfig } from './services/config.js';
import { tracingMiddleware, structuredLogger } from './middleware/tracing.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

async function main() {
  const config = await loadConfig();
  const app = express();
  const port = config.integrationApi.port;

  // Middleware
  app.use(helmet({
    crossOriginResourcePolicy: { policy: "cross-origin" }
  }));
  app.use(cors());
  app.use(tracingMiddleware);
  app.use(morgan(config.integrationApi.logLevel === 'debug' ? 'dev' : 'combined'));
  app.use(express.json());

  // Health check
  app.get('/health', (req, res) => {
    res.json({ status: 'ok', timestamp: new Date().toISOString() });
  });

  // Routes
  app.use('/api/config', configRouter);
  app.use('/api/events', eventsRouter);
  app.use('/api/tests', testsRouter);
  app.use('/api/commands', commandsRouter);
  app.use('/api/mcp', mcpRouter);
  app.use('/api/health', healthRouter);

  // Error handler
  app.use((err: Error, req: express.Request, res: express.Response, next: express.NextFunction) => {
    console.error('Error:', err);
    res.status(500).json({ error: err.message || 'Internal server error' });
  });

  app.listen(port, () => {
    structuredLogger('Integration API started', 'info', {
      port,
      environment: process.env.NODE_ENV || 'development',
      endpoints: {
        health: `/health`,
        config: `/api/config`,
        events: `/api/events/*`,
        heartbeat: `/api/events/heartbeat`
      }
    });
  });
}

main().catch((error) => {
  structuredLogger('Failed to start Integration API', 'error', { error: error.message, stack: error.stack });
  process.exit(1);
});
