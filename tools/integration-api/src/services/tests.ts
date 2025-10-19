import fs from 'fs/promises';
import path from 'path';
import { fileURLToPath } from 'url';
import { spawn, ChildProcess } from 'child_process';
import { v4 as uuidv4 } from 'uuid';
import type { TestScenario, TestRun } from '../types/index.js';
import { getConfig } from './config.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT_DIR = path.resolve(__dirname, '../../../..');

const activeRuns = new Map<string, { process: ChildProcess; run: TestRun; logs: string[] }>();

export async function listScenarios(): Promise<TestScenario[]> {
  const config = getConfig();
  const scenariosPath = path.join(ROOT_DIR, config.testClient.scenariosPath);

  try {
    const files = await fs.readdir(scenariosPath);
    const scenarios: TestScenario[] = [];

    for (const file of files) {
      if (file.endsWith('.js') && !file.startsWith('.')) {
        const filePath = path.join(scenariosPath, file);
        const stat = await fs.stat(filePath);
        
        if (stat.isFile()) {
          const content = await fs.readFile(filePath, 'utf-8');
          const descMatch = content.match(/\* (.+?)$/m);
          
          scenarios.push({
            id: file.replace('.js', ''),
            name: file.replace('.js', '').replace(/-/g, ' '),
            description: descMatch ? descMatch[1] : 'Test scenario',
            script: file,
            path: filePath
          });
        }
      }
    }

    // Check for subdirectories (like mcp/)
    for (const file of files) {
      const filePath = path.join(scenariosPath, file);
      const stat = await fs.stat(filePath);
      
      if (stat.isDirectory()) {
        try {
          const subFiles = await fs.readdir(filePath);
          for (const subFile of subFiles) {
            if (subFile.endsWith('.js') && !subFile.startsWith('.')) {
              const subFilePath = path.join(filePath, subFile);
              const content = await fs.readFile(subFilePath, 'utf-8');
              const descMatch = content.match(/\* (.+?)$/m);
              
              scenarios.push({
                id: `${file}/${subFile.replace('.js', '')}`,
                name: `${file}/${subFile.replace('.js', '')}`.replace(/-/g, ' '),
                description: descMatch ? descMatch[1] : 'Test scenario',
                script: `${file}/${subFile}`,
                path: subFilePath
              });
            }
          }
        } catch (err) {
          // Ignore errors reading subdirectories
        }
      }
    }

    return scenarios;
  } catch (error: any) {
    console.error('Error listing scenarios:', error);
    return [];
  }
}

export async function runTest(scenarioId: string | 'all'): Promise<string> {
  const runId = uuidv4();
  const config = getConfig();
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-').replace('T', '_');
  const artifactsPath = path.join(ROOT_DIR, config.testClient.artifactsDir, timestamp, 'runs', runId);

  await fs.mkdir(artifactsPath, { recursive: true });

  const run: TestRun = {
    runId,
    scenarioId,
    startTime: new Date().toISOString(),
    status: 'running',
    artifactsPath
  };

  let command: string;
  let args: string[];

  if (scenarioId === 'all') {
    command = 'npm';
    args = ['run', 'test:all'];
  } else {
    // Find the scenario
    const scenarios = await listScenarios();
    const scenario = scenarios.find(s => s.id === scenarioId);
    
    if (!scenario) {
      throw new Error(`Scenario not found: ${scenarioId}`);
    }

    command = 'node';
    args = [scenario.path];
  }

  const testClientDir = path.join(ROOT_DIR, 'server-dotnet/operator/test-client');
  const logFile = path.join(artifactsPath, 'test-client.log');

  const proc = spawn(command, args, {
    cwd: testClientDir,
    env: {
      ...process.env,
      ARTIFACTS_DIR: artifactsPath,
      OPERATOR_URL: config.roomOperator.baseUrl,
      ROOMSERVER_URL: config.roomServer.baseUrl
    }
  });

  const logs: string[] = [];

  proc.stdout?.on('data', (data) => {
    const text = data.toString();
    logs.push(text);
    fs.appendFile(logFile, text).catch(console.error);
  });

  proc.stderr?.on('data', (data) => {
    const text = data.toString();
    logs.push(text);
    fs.appendFile(logFile, text).catch(console.error);
  });

  proc.on('close', async (code) => {
    run.endTime = new Date().toISOString();
    run.status = code === 0 ? 'completed' : 'failed';
    run.exitCode = code ?? undefined;

    // Save result metadata
    const resultPath = path.join(artifactsPath, 'result.json');
    await fs.writeFile(resultPath, JSON.stringify(run, null, 2));

    activeRuns.delete(runId);
  });

  activeRuns.set(runId, { process: proc, run, logs });

  return runId;
}

export function getRun(runId: string): TestRun | null {
  const active = activeRuns.get(runId);
  return active ? active.run : null;
}

export function getRunLogs(runId: string): string[] {
  const active = activeRuns.get(runId);
  return active ? active.logs : [];
}

export async function getRunMetadata(runId: string): Promise<any> {
  const config = getConfig();
  const artifactsDir = path.join(ROOT_DIR, config.testClient.artifactsDir);
  
  // Search for the run in artifacts
  try {
    const dates = await fs.readdir(artifactsDir);
    for (const date of dates) {
      const runsDir = path.join(artifactsDir, date, 'runs', runId);
      try {
        const resultPath = path.join(runsDir, 'result.json');
        const content = await fs.readFile(resultPath, 'utf-8');
        return JSON.parse(content);
      } catch (err) {
        // Continue searching
      }
    }
  } catch (error) {
    // Artifacts directory doesn't exist yet
  }

  // Check if it's an active run
  const active = activeRuns.get(runId);
  if (active) {
    return active.run;
  }

  return null;
}
