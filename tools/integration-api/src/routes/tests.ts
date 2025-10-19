import { Router } from 'express';
import type { Request, Response } from 'express';
import { listScenarios, runTest, getRun, getRunLogs, getRunMetadata } from '../services/tests.js';
import { getConfig } from '../services/config.js';

export const testsRouter = Router();

// GET /api/tests - List available test scenarios
testsRouter.get('/', async (req, res) => {
  try {
    const scenarios = await listScenarios();
    res.json({ scenarios });
  } catch (error: any) {
    res.status(500).json({ error: error.message });
  }
});

// POST /api/tests/run - Execute a test scenario
testsRouter.post('/run', async (req, res) => {
  try {
    const { scenarioId = 'all' } = req.body;
    const runId = await runTest(scenarioId);
    res.json({ runId, status: 'started' });
  } catch (error: any) {
    res.status(500).json({ error: error.message });
  }
});

// GET /api/tests/runs/:runId - Get test run metadata
testsRouter.get('/runs/:runId', async (req, res) => {
  try {
    const { runId } = req.params;
    const metadata = await getRunMetadata(runId);
    
    if (!metadata) {
      return res.status(404).json({ error: 'Run not found' });
    }
    
    res.json(metadata);
  } catch (error: any) {
    res.status(500).json({ error: error.message });
  }
});

// GET /api/tests/stream/:runId - SSE stream of test logs
testsRouter.get('/stream/:runId', async (req: Request, res: Response) => {
  const { runId } = req.params;
  const config = getConfig();
  const retryInterval = config.ui?.sseReconnectInterval || 5000;
  
  res.setHeader('Content-Type', 'text/event-stream');
  res.setHeader('Cache-Control', 'no-cache');
  res.setHeader('Connection', 'keep-alive');
  res.setHeader('X-Accel-Buffering', 'no');
  res.write(`retry: ${retryInterval}\n\n`);
  res.flushHeaders();

  // Send initial connection message
  res.write(`data: ${JSON.stringify({ type: 'connected', runId })}\n\n`);

  // Get the run
  const run = getRun(runId);
  if (!run) {
    res.write(`data: ${JSON.stringify({ type: 'error', message: 'Run not found' })}\n\n`);
    return res.end();
  }

  // Stream existing logs
  const logs = getRunLogs(runId);
  for (const log of logs) {
    res.write(`data: ${JSON.stringify({ type: 'log', data: log })}\n\n`);
  }

  // Set up polling for new logs
  let lastLogCount = logs.length;
  const pollInterval = setInterval(() => {
    const currentLogs = getRunLogs(runId);
    if (currentLogs.length > lastLogCount) {
      const newLogs = currentLogs.slice(lastLogCount);
      for (const log of newLogs) {
        res.write(`data: ${JSON.stringify({ type: 'log', data: log })}\n\n`);
      }
      lastLogCount = currentLogs.length;
    }

    // Check if run is complete
    const currentRun = getRun(runId);
    if (!currentRun || currentRun.status !== 'running') {
      res.write(`data: ${JSON.stringify({ type: 'complete', status: currentRun?.status })}\n\n`);
      clearInterval(pollInterval);
      res.end();
    }
  }, 1000);

  // Heartbeat
  const heartbeat = setInterval(() => {
    res.write(': ping\n\n');
  }, 10000);

  req.on('close', () => {
    clearInterval(pollInterval);
    clearInterval(heartbeat);
  });
});
