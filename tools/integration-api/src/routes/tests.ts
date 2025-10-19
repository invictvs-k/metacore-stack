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
    const { scenarioId, all } = req.body;
    const testId = all ? 'all' : (scenarioId || 'all');
    const result = await runTest(testId);
    res.json(result);
  } catch (error: any) {
    res.status(500).json({ 
      error: error.message,
      action: 'Check that the test runner exists and scenarios path is valid'
    });
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

  // Get the run
  const runData = getRun(runId);
  if (!runData) {
    res.write(`event: error\n`);
    res.write(`data: ${JSON.stringify({ runId, message: 'Run not found' })}\n\n`);
    return res.end();
  }

  // Send started event
  res.write(`event: started\n`);
  res.write(`data: ${JSON.stringify({ runId, artifactsDir: runData.artifactsPath })}\n\n`);

  // Stream existing logs
  const logs = getRunLogs(runId);
  for (const log of logs) {
    res.write(`event: log\n`);
    res.write(`data: ${JSON.stringify({ runId, chunk: log })}\n\n`);
  }

  // Set up polling for new logs
  let lastLogCount = logs.length;
  let completed = false;
  let isPolling = false;
  let heartbeat: NodeJS.Timeout | null = null;

  const finalizeRun = (exitCode: number) => {
    if (completed) {
      return;
    }

    completed = true;
    res.write(`event: done\n`);
    res.write(`data: ${JSON.stringify({ runId, exitCode })}\n\n`);
    clearInterval(pollInterval);
    if (heartbeat) {
      clearInterval(heartbeat);
    }
    res.end();
  };

  const pollForUpdates = async () => {
    const currentLogs = getRunLogs(runId);
    if (currentLogs.length > lastLogCount) {
      const newLogs = currentLogs.slice(lastLogCount);
      for (const log of newLogs) {
        res.write(`event: log\n`);
        res.write(`data: ${JSON.stringify({ runId, chunk: log })}\n\n`);
      }
      lastLogCount = currentLogs.length;
    }

    // Check if run is complete
    const currentRun = getRun(runId);
    if (!currentRun || currentRun.status !== 'running') {
      let exitCode: number;

      if (currentRun?.exitCode !== undefined) {
        exitCode = currentRun.exitCode;
      } else {
        try {
          const metadata = await getRunMetadata(runId);
          exitCode = metadata?.exitCode ?? -1;
        } catch (error) {
          exitCode = -1;
        }
      }

      finalizeRun(exitCode);
    }
  };

  const pollInterval = setInterval(() => {
    if (completed || isPolling) {
      return;
    }

    isPolling = true;

    pollForUpdates()
      .catch(() => {
        finalizeRun(-1);
      })
      .finally(() => {
        isPolling = false;
      });
  }, 500); // Poll more frequently for logs (500ms)

  // Heartbeat
  heartbeat = setInterval(() => {
    res.write(': ping\n\n');
  }, 10000);

  req.on('close', () => {
    clearInterval(pollInterval);
    if (heartbeat) {
      clearInterval(heartbeat);
      heartbeat = null;
    }
  });
});
