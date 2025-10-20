#!/usr/bin/env node

/**
 * Contract validation script
 * Validates example data against JSON schemas
 */

import Ajv from 'ajv';
import { readFileSync } from 'fs';
import { resolve } from 'path';

const ajv = new Ajv({ allErrors: true, strict: false });

console.log('🔍 Validating contracts...\n');

// Test SSE Events Schema
const sseSchema = JSON.parse(
  readFileSync(resolve('configs/schemas/sse.events.schema.json'), 'utf-8')
);

const sseExamples = [
  {
    event: 'log',
    data: 'Test log message',
    timestamp: new Date().toISOString()
  },
  {
    event: 'heartbeat',
    data: { count: 1, message: 'ping' },
    timestamp: new Date().toISOString(),
    traceId: 'test-123'
  },
  {
    event: 'done',
    data: 'Completed',
    timestamp: new Date().toISOString()
  }
];

const validateSSE = ajv.compile(sseSchema);
let sseValid = true;

for (const example of sseExamples) {
  const valid = validateSSE(example);
  if (valid) {
    console.log(`✅ SSE Event '${example.event}' valid`);
  } else {
    console.error(`❌ SSE Event '${example.event}' invalid:`, validateSSE.errors);
    sseValid = false;
  }
}

// Test Commands Catalog Schema
const commandsSchema = JSON.parse(
  readFileSync(resolve('configs/schemas/commands.catalog.schema.json'), 'utf-8')
);

const commandsExample = {
  version: '0.1.0',
  commands: [
    {
      name: 'room.create',
      description: 'Create a new room',
      params: {
        type: 'object',
        properties: {
          name: { type: 'string' }
        },
        required: ['name']
      },
      category: 'room'
    }
  ]
};

const validateCommands = ajv.compile(commandsSchema);
const commandsValid = validateCommands(commandsExample);

if (commandsValid) {
  console.log('✅ Commands Catalog valid');
} else {
  console.error('❌ Commands Catalog invalid:', validateCommands.errors);
}

// Summary
console.log('\n' + '='.repeat(50));
if (sseValid && commandsValid) {
  console.log('✅ All contract validations PASSED');
  process.exit(0);
} else {
  console.log('❌ Some contract validations FAILED');
  process.exit(1);
}
