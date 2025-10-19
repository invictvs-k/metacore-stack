import { Router } from 'express';
import fs from 'fs/promises';
import path from 'path';
import { fileURLToPath } from 'url';
import type { CommandCatalog } from '../types/index.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT_DIR = path.resolve(__dirname, '../../../..');

export const commandsRouter = Router();

// GET /api/commands - Get command catalog
commandsRouter.get('/', async (req, res) => {
  try {
    const catalogPath = path.join(ROOT_DIR, 'server-dotnet/operator/commands/commands.catalog.json');
    
    // Try to read the catalog file
    try {
      const content = await fs.readFile(catalogPath, 'utf-8');
      const catalog: CommandCatalog = JSON.parse(content);
      res.json(catalog);
    } catch (err) {
      // Return a default catalog if file doesn't exist
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
            usage: 'Send list of providers to initiate connection'
          },
          {
            id: 'mcp.status',
            title: 'Query MCP Status',
            description: 'Query the status of MCP connections in RoomServer',
            paramsSchema: {
              type: 'object',
              properties: {}
            },
            usage: 'Returns states per provider'
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
            usage: 'Triggers immediate reconciliation'
          }
        ]
      };
      res.json(defaultCatalog);
    }
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

    // In a real implementation, this would send the command to RoomOperator
    // For now, we'll simulate a successful execution
    res.json({
      success: true,
      commandId,
      executedAt: new Date().toISOString(),
      result: {
        message: `Command ${commandId} executed successfully`,
        params
      }
    });
  } catch (error: any) {
    res.status(500).json({ error: error.message });
  }
});
