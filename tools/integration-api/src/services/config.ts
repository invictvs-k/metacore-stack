import fs from 'fs/promises';
import path from 'path';
import { fileURLToPath } from 'url';
import crypto from 'crypto';
import type { DashboardSettings } from '../types/index.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const CONFIG_PATH = path.resolve(__dirname, '../../../../configs/dashboard.settings.json');

let cachedConfig: DashboardSettings | null = null;
let configChecksum: string | null = null;

export async function loadConfig(): Promise<DashboardSettings> {
  try {
    const content = await fs.readFile(CONFIG_PATH, 'utf-8');
    cachedConfig = JSON.parse(content);
    configChecksum = crypto.createHash('md5').update(content).digest('hex');
    return cachedConfig as DashboardSettings;
  } catch (error) {
    console.error('Failed to load config, using defaults:', error);
    // Return default config
    cachedConfig = {
      version: 1,
      roomServer: {
        baseUrl: 'http://127.0.0.1:40801',
        events: { type: 'sse', path: '/events' }
      },
      roomOperator: {
        baseUrl: 'http://127.0.0.1:40802',
        events: { type: 'sse', path: '/events' }
      },
      testClient: {
        runner: 'scripts/run-test-client.sh',
        scenariosPath: 'server-dotnet/operator/test-client/scenarios',
        artifactsDir: '.artifacts/integration'
      },
      integrationApi: {
        port: 40901,
        logLevel: 'info'
      },
      ui: {
        theme: 'system'
      }
    };
    return cachedConfig;
  }
}

export async function saveConfig(config: DashboardSettings): Promise<void> {
  const content = JSON.stringify(config, null, 2);
  await fs.mkdir(path.dirname(CONFIG_PATH), { recursive: true });
  await fs.writeFile(CONFIG_PATH, content, 'utf-8');
  cachedConfig = config;
  configChecksum = crypto.createHash('md5').update(content).digest('hex');
}

export function getConfig(): DashboardSettings {
  if (!cachedConfig) {
    throw new Error('Config not loaded');
  }
  return cachedConfig;
}

export function getConfigChecksum(): string {
  return configChecksum || '';
}
