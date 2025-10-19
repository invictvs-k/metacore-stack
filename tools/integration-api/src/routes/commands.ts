import { Router } from 'express';
import fs from 'fs/promises';
import path from 'path';
import { fileURLToPath } from 'url';
import fetch from 'node-fetch';
import type { CommandCatalog, Command } from '../types/index.js';
import { getConfig } from '../services/config.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT_DIR = path.resolve(__dirname, '../../../..');

export const commandsRouter = Router();

const CATALOG_PATH = path.join(ROOT_DIR, 'server-dotnet/operator/commands/commands.catalog.json');

const defaultCatalog: CommandCatalog = {
  version: 1,
  commands: [
    {
      id: 'mcp.load',
      title: 'Load MCP Providers',
      description: 'Load and start connection with configured MCP providers',
      paramsSchema: {
        type: 'object',
        properties: {
          providers: {
            type: 'array',
            items: {
              type: 'object',
              properties: {
                id: { type: 'string' },
                url: { type: 'string' },
                token: { type: 'string' }
              },
              required: ['id', 'url']
            }
          }
        },
        required: ['providers']
      },
      usage: 'Send list of providers to initiate connection',
      method: 'POST',
      endpoint: '/mcp/load'
    },
    {
      id: 'mcp.status',
      title: 'Query MCP Status',
      description: 'Query the status of MCP connections in RoomServer',
      paramsSchema: {
        type: 'object',
        properties: {}
      },
      usage: 'Returns states per provider',
      method: 'GET',
      endpoint: '/mcp/status'
    },
    {
      id: 'operator.reconcile',
      title: 'Trigger Reconciliation',
      description: 'Manually trigger a reconciliation cycle',
      paramsSchema: {
        type: 'object',
        properties: {
          force: { type: 'boolean' }
        }
      },
      usage: 'Triggers immediate reconciliation',
      method: 'POST',
      endpoint: '/apply'
    }
  ]
};

async function loadCatalog(): Promise<CommandCatalog> {
  try {
    const content = await fs.readFile(CATALOG_PATH, 'utf-8');
    const catalog: CommandCatalog = JSON.parse(content);
    const commands = (catalog.commands ?? defaultCatalog.commands).map((cmd) => {
      const fallback = defaultCatalog.commands.find((item) => item.id === cmd.id);
      return fallback ? { ...fallback, ...cmd } : cmd;
    });

    return {
      ...defaultCatalog,
      ...catalog,
      commands
    };
  } catch (error: any) {
    if (error.code !== 'ENOENT') {
      console.warn('Failed to read command catalog, using defaults:', error.message);
    }
    return defaultCatalog;
  }
}

function resolveCommandMetadata(catalog: CommandCatalog, commandId: string): Command | undefined {
  return catalog.commands.find((cmd) => cmd.id === commandId);
}

// GET /api/commands - Get command catalog
commandsRouter.get('/', async (req, res) => {
  try {
    const catalog = await loadCatalog();
    res.json(catalog);
  } catch (error: any) {
    res.status(500).json({ error: error.message });
  }
});

// POST /api/commands/execute - Execute a command
commandsRouter.post('/execute', async (req, res) => {
  try {
    const { commandId, params } = req.body;

    if (!commandId) {
      return res.status(400).json({ error: 'commandId is required' });
    }

    const catalog = await loadCatalog();
    const command = resolveCommandMetadata(catalog, commandId);

    if (!command) {
      return res.status(404).json({ error: `Command ${commandId} not found` });
    }

    const config = getConfig();
    const method = (command.method ?? 'POST').toUpperCase();
    const endpoint = command.endpoint ?? `/${commandId.replace(/\./g, '/')}`;
    const baseUrl = config.roomOperator.baseUrl.replace(/\/$/, '');
    const url = new URL(endpoint, `${baseUrl}/`).toString();

    const hasBody = ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method);

    let finalUrl = url;
    let body: string | undefined;

    if (hasBody) {
      body = JSON.stringify(params ?? {});
    } else if (params && Object.keys(params).length > 0) {
      const urlObj = new URL(url);
      const search = new URLSearchParams();
      for (const [key, value] of Object.entries(params)) {
        search.append(key, typeof value === 'string' ? value : JSON.stringify(value));
      }
      urlObj.search = search.toString();
      finalUrl = urlObj.toString();
    }

    const response = await fetch(finalUrl, {
      method,
      headers: {
        'Content-Type': 'application/json'
      },
      body: hasBody ? body : undefined
    });

    const contentType = response.headers.get('content-type') ?? '';
    const isJson = contentType.includes('application/json');
    const responseBody = isJson ? await response.json() : await response.text();

    if (!response.ok) {
      if (isJson) {
        return res.status(response.status).json(responseBody);
      }
      return res.status(response.status).json({ error: responseBody });
    }

    if (isJson) {
      return res.status(response.status).json(responseBody);
    }

    return res.status(response.status).json({ result: responseBody });
  } catch (error: any) {
    res.status(500).json({ error: error.message });
  }
});
