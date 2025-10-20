import fs from 'fs/promises';
import path from 'path';
import { fileURLToPath } from 'url';
import crypto from 'crypto';
import type { DashboardSettings } from '../types/index.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const CONFIG_PATH = path.resolve(__dirname, '../../../../configs/dashboard.settings.json');

// Default configuration values grouped together for better organization
const DEFAULT_CONFIG = {
  SSE_RECONNECT_INTERVAL: 5000, // 5 seconds
  SSE_MAX_RECONNECT_INTERVAL: 30000, // 30 seconds
  SSE_RECONNECT_BACKOFF_MULTIPLIER: 1.5,
} as const;

let cachedConfig: DashboardSettings | null = null;
let configChecksum: string | null = null;

function validateConfig(config: any): { valid: boolean; errors: string[] } {
  const errors: string[] = [];

  // Validate URLs
  try {
    new URL(config.roomServer?.baseUrl || '');
  } catch {
    errors.push('roomServer.baseUrl must be a valid URL');
  }

  try {
    new URL(config.roomOperator?.baseUrl || '');
  } catch {
    errors.push('roomOperator.baseUrl must be a valid URL');
  }

  // Validate port
  const port = config.integrationApi?.port;
  if (!Number.isInteger(port) || port < 1 || port > 65535) {
    errors.push('integrationApi.port must be an integer between 1 and 65535');
  }

  // Validate runner exists (basic check - actual execution will verify)
  if (!config.testClient?.runner) {
    errors.push('testClient.runner must be specified');
  }

  // Validate scenarios path (will check existence if needed)
  if (!config.testClient?.scenariosPath) {
    errors.push('testClient.scenariosPath must be specified');
  }

  return {
    valid: errors.length === 0,
    errors
  };
}

export async function loadConfig(): Promise<DashboardSettings> {
  try {
    const content = await fs.readFile(CONFIG_PATH, 'utf-8');
    const config = JSON.parse(content);
    
    // Validate config
    const validation = validateConfig(config);
    if (!validation.valid) {
      console.warn('Config validation warnings:', validation.errors);
    }
    
    cachedConfig = config;
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
        theme: 'system',
        sseReconnectInterval: DEFAULT_CONFIG.SSE_RECONNECT_INTERVAL,
        sseMaxReconnectInterval: DEFAULT_CONFIG.SSE_MAX_RECONNECT_INTERVAL,
        sseReconnectBackoffMultiplier: DEFAULT_CONFIG.SSE_RECONNECT_BACKOFF_MULTIPLIER
      }
    };
    return cachedConfig;
  }
}

export async function saveConfig(config: DashboardSettings): Promise<void> {
  // Validate before saving
  const validation = validateConfig(config);
  if (!validation.valid) {
    throw new Error(`Configuration validation failed: ${validation.errors.join(', ')}`);
  }

  const content = JSON.stringify(config, null, 2);
  await fs.mkdir(path.dirname(CONFIG_PATH), { recursive: true });
  await fs.writeFile(CONFIG_PATH, content, 'utf-8');
  
  // Reload config after saving
  const oldChecksum = configChecksum;
  cachedConfig = config;
  configChecksum = crypto.createHash('md5').update(content).digest('hex');
  
  console.log('Config saved and reloaded. Hot reload triggered.');
  if (oldChecksum !== configChecksum) {
    console.log('Config version changed:', { old: oldChecksum, new: configChecksum });
  }
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
