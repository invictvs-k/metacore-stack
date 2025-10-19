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
import { loadConfig } from './services/config.js';

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

  // Error handler
  app.use((err: Error, req: express.Request, res: express.Response, next: express.NextFunction) => {
    console.error('Error:', err);
    res.status(500).json({ error: err.message || 'Internal server error' });
  });

  app.listen(port, () => {
    console.log(`Integration API listening on port ${port}`);
    console.log(`Health check: http://localhost:${port}/health`);
  });
}

main().catch(console.error);
