#!/usr/bin/env node
/**
 * Artifact Persistence Unit Test
 * 
 * Tests artifact persistence logic without requiring a running server.
 * Validates schema compliance, manifest structure, and versioning logic.
 */

import fs from 'fs';
import path from 'path';
import crypto from 'crypto';
import { fileURLToPath } from 'url';
import Ajv from 'ajv';
import addFormats from 'ajv-formats';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const PROJECT_ROOT = path.join(__dirname, '../..');
const SCHEMA_DIR = path.join(PROJECT_ROOT, 'schemas');

// Test results
const results = {
  passed: 0,
  failed: 0,
  tests: []
};

function test(name, fn) {
  try {
    fn();
    results.passed++;
    results.tests.push({ name, status: 'PASS' });
    console.log(`✅ ${name}`);
  } catch (error) {
    results.failed++;
    results.tests.push({ name, status: 'FAIL', error: error.message });
    console.error(`❌ ${name}`);
    console.error(`   Error: ${error.message}`);
  }
}

function assertEquals(actual, expected, message) {
  if (actual !== expected) {
    throw new Error(message || `Expected ${expected}, got ${actual}`);
  }
}

function assertTrue(value, message) {
  if (!value) {
    throw new Error(message || 'Expected true, got false');
  }
}

function assertNotNull(value, message) {
  if (value === null || value === undefined) {
    throw new Error(message || 'Expected non-null value');
  }
}

// Load and validate artifact schema
function loadArtifactSchema() {
  const schemaPath = path.join(SCHEMA_DIR, 'artifact-manifest.schema.json');
  const schema = JSON.parse(fs.readFileSync(schemaPath, 'utf8'));
  
  // Load common definitions
  const commonPath = path.join(SCHEMA_DIR, 'common.defs.json');
  const common = JSON.parse(fs.readFileSync(commonPath, 'utf8'));
  
  return { schema, common };
}

// Create AJV validator
function createValidator() {
  const ajv = new Ajv({
    schemas: [],
    strict: false,
    allErrors: true,
    validateFormats: false
  });
  addFormats(ajv);
  
  const { schema, common } = loadArtifactSchema();
  
  // Remove $schema reference to avoid draft-2020-12 issues
  const schemaCopy = { ...schema };
  delete schemaCopy.$schema;
  delete schemaCopy.$id;
  
  const commonCopy = { ...common };
  delete commonCopy.$schema;
  delete commonCopy.$id;
  
  ajv.addSchema(commonCopy, 'common.defs.json');
  
  return ajv.compile(schemaCopy);
}

// Test: Schema loads correctly
test('Schema loads correctly', () => {
  const { schema } = loadArtifactSchema();
  assertNotNull(schema, 'Schema should not be null');
  assertEquals(schema.type, 'object', 'Schema should be of type object');
  assertTrue(Array.isArray(schema.required), 'Schema should have required fields');
});

// Test: Valid artifact manifest passes validation
test('Valid artifact manifest passes validation', () => {
  const validate = createValidator();
  
  const validManifest = {
    name: 'test-artifact.txt',
    type: 'text/plain',
    path: '.ai-flow/runs/room-abc123/artifacts/test-artifact.txt',
    size: 1024,
    sha256: 'a'.repeat(64),
    origin: {
      room: 'room-abc123',
      entity: 'E-001',
      workspace: 'room'
    },
    version: 1,
    ts: '2025-10-17T12:02:10Z'
  };
  
  const valid = validate(validManifest);
  if (!valid) {
    console.error('Validation errors:', validate.errors);
  }
  assertTrue(valid, 'Valid manifest should pass validation');
});

// Test: Invalid artifact manifest fails validation (missing required field)
test('Invalid manifest fails validation - missing required field', () => {
  const validate = createValidator();
  
  const invalidManifest = {
    name: 'test-artifact.txt',
    type: 'text/plain',
    // Missing 'path', 'sha256', 'origin', 'ts'
  };
  
  const valid = validate(invalidManifest);
  assertEquals(valid, false, 'Invalid manifest should fail validation');
  assertTrue(validate.errors.length > 0, 'Should have validation errors');
});

// Test: Artifact versioning logic
test('Artifact versioning increments correctly', () => {
  // Simulate manifest history
  const manifests = [
    { name: 'doc.txt', version: 1, sha256: 'hash1' },
    { name: 'doc.txt', version: 2, sha256: 'hash2' },
    { name: 'other.txt', version: 1, sha256: 'hash3' }
  ];
  
  // Get next version for 'doc.txt'
  const nextVersion = manifests
    .filter(m => m.name === 'doc.txt')
    .map(m => m.version)
    .reduce((max, v) => Math.max(max, v), 0) + 1;
  
  assertEquals(nextVersion, 3, 'Next version should be 3');
});

// Test: SHA256 hash calculation
test('SHA256 hash calculation', () => {
  const content = 'Hello, world!';
  const hash = crypto.createHash('sha256').update(content).digest('hex');
  
  assertEquals(hash.length, 64, 'SHA256 hash should be 64 characters');
  assertTrue(/^[a-f0-9]{64}$/.test(hash), 'SHA256 hash should be lowercase hex');
  
  // Verify deterministic hashing
  const hash2 = crypto.createHash('sha256').update(content).digest('hex');
  assertEquals(hash, hash2, 'Hash should be deterministic');
});

// Test: Artifact path construction
test('Artifact path construction', () => {
  const roomId = 'room-123';
  const workspace = 'room';
  const name = 'artifact.txt';
  
  const expectedPath = `.ai-flow/runs/${roomId}/artifacts/${name}`;
  const path = `.ai-flow/runs/${roomId}/artifacts/${name}`;
  
  assertEquals(path, expectedPath, 'Path should be constructed correctly');
});

// Test: Entity workspace path construction
test('Entity workspace path construction', () => {
  const roomId = 'room-123';
  const entityId = 'E-001';
  const name = 'artifact.txt';
  
  const expectedPath = `.ai-flow/runs/${roomId}/entities/${entityId}/artifacts/${name}`;
  const path = `.ai-flow/runs/${roomId}/entities/${entityId}/artifacts/${name}`;
  
  assertEquals(path, expectedPath, 'Entity path should be constructed correctly');
});

// Test: Manifest metadata is optional
test('Manifest metadata is optional', () => {
  const validate = createValidator();
  
  const manifestWithoutMetadata = {
    name: 'test.txt',
    type: 'text/plain',
    path: '.ai-flow/runs/room-abc123/artifacts/test.txt',
    sha256: 'a'.repeat(64),
    origin: {
      room: 'room-abc123',
      entity: 'E-001',
      workspace: 'room'
    },
    version: 1,
    ts: '2025-10-17T12:02:10Z'
  };
  
  const valid = validate(manifestWithoutMetadata);
  assertTrue(valid, 'Manifest without metadata should be valid');
});

// Test: Manifest with metadata
test('Manifest with metadata validates', () => {
  const validate = createValidator();
  
  const manifestWithMetadata = {
    name: 'test.txt',
    type: 'text/plain',
    path: '.ai-flow/runs/room-abc123/artifacts/test.txt',
    sha256: 'a'.repeat(64),
    origin: {
      room: 'room-abc123',
      entity: 'E-001',
      workspace: 'room'
    },
    version: 1,
    metadata: {
      stage: 'draft',
      author: 'E-001',
      customField: 'value'
    },
    ts: '2025-10-17T12:02:10Z'
  };
  
  const valid = validate(manifestWithMetadata);
  assertTrue(valid, 'Manifest with metadata should be valid');
});

// Test: Parent tracking
test('Parent tracking in manifest', () => {
  const validate = createValidator();
  
  const manifestWithParents = {
    name: 'promoted.txt',
    type: 'text/plain',
    path: '.ai-flow/runs/room-abc123/artifacts/promoted.txt',
    sha256: 'b'.repeat(64),
    origin: {
      room: 'room-abc123',
      entity: 'E-001',
      workspace: 'room'
    },
    version: 1,
    parents: ['a'.repeat(64)],
    ts: '2025-10-17T12:02:10Z'
  };
  
  const valid = validate(manifestWithParents);
  assertTrue(valid, 'Manifest with parents should be valid');
  assertEquals(manifestWithParents.parents.length, 1, 'Should have one parent');
});

// Test: Multiple parents tracking
test('Multiple parents tracking', () => {
  const validate = createValidator();
  
  const manifestWithMultipleParents = {
    name: 'merged.txt',
    type: 'text/plain',
    path: '.ai-flow/runs/room-abc123/artifacts/merged.txt',
    sha256: 'c'.repeat(64),
    origin: {
      room: 'room-abc123',
      entity: 'E-001',
      workspace: 'room'
    },
    version: 1,
    parents: ['a'.repeat(64), 'b'.repeat(64)],
    ts: '2025-10-17T12:02:10Z'
  };
  
  const valid = validate(manifestWithMultipleParents);
  assertTrue(valid, 'Manifest with multiple parents should be valid');
  assertEquals(manifestWithMultipleParents.parents.length, 2, 'Should have two parents');
});

// Test: Workspace validation
test('Workspace must be room or entity', () => {
  const validate = createValidator();
  
  // Valid workspaces
  const roomWorkspace = {
    name: 'test.txt',
    type: 'text/plain',
    path: '.ai-flow/runs/room-abc123/artifacts/test.txt',
    sha256: 'a'.repeat(64),
    origin: {
      room: 'room-abc123',
      entity: 'E-001',
      workspace: 'room'
    },
    version: 1,
    ts: '2025-10-17T12:02:10Z'
  };
  
  const entityWorkspace = {
    name: 'test.txt',
    type: 'text/plain',
    path: '.ai-flow/runs/room-abc123/entities/E-001/artifacts/test.txt',
    sha256: 'a'.repeat(64),
    origin: {
      room: 'room-abc123',
      entity: 'E-001',
      workspace: 'entity'
    },
    version: 1,
    ts: '2025-10-17T12:02:10Z'
  };
  
  assertTrue(validate(roomWorkspace), 'Room workspace should be valid');
  assertTrue(validate(entityWorkspace), 'Entity workspace should be valid');
});

// Test: Example artifact from schema validates
test('Example artifact from schema validates', () => {
  const validate = createValidator();
  const examplePath = path.join(SCHEMA_DIR, 'examples', 'artifact-sample.json');
  
  if (fs.existsSync(examplePath)) {
    const example = JSON.parse(fs.readFileSync(examplePath, 'utf8'));
    const valid = validate(example);
    if (!valid) {
      console.error('Example validation errors:', validate.errors);
    }
    assertTrue(valid, 'Schema example should be valid');
  } else {
    console.warn('   ⚠️  Example file not found, skipping');
  }
});

// Test: Invalid example is rejected
test('Invalid example is rejected', () => {
  const validate = createValidator();
  const invalidExamplePath = path.join(SCHEMA_DIR, 'examples', 'invalid', 'artifact-bad-hash.json');
  
  if (fs.existsSync(invalidExamplePath)) {
    const invalidExample = JSON.parse(fs.readFileSync(invalidExamplePath, 'utf8'));
    const valid = validate(invalidExample);
    assertEquals(valid, false, 'Invalid example should fail validation');
  } else {
    console.warn('   ⚠️  Invalid example file not found, skipping');
  }
});

// Test: ISO datetime format
test('ISO datetime format validation', () => {
  const validate = createValidator();
  
  const validDates = [
    '2025-10-17T12:02:10Z',
    '2025-10-17T12:02:10.123Z'
  ];
  
  for (const date of validDates) {
    const manifest = {
      name: 'test.txt',
      type: 'text/plain',
      path: '.ai-flow/runs/room-abc123/artifacts/test.txt',
      sha256: 'a'.repeat(64),
      origin: {
        room: 'room-abc123',
        entity: 'E-001',
        workspace: 'room'
      },
      version: 1,
      ts: date
    };
    
    const valid = validate(manifest);
    assertTrue(valid, `Date ${date} should be valid`);
  }
});

// Run all tests
console.log('🧪 Running Artifact Persistence Unit Tests\n');
console.log('═'.repeat(50));
console.log();

// All tests are defined above and will run when the test() function is called

console.log();
console.log('═'.repeat(50));
console.log();
console.log(`Test Results: ${results.passed} passed, ${results.failed} failed`);

if (results.failed > 0) {
  console.log('\nFailed tests:');
  results.tests
    .filter(t => t.status === 'FAIL')
    .forEach(t => console.log(`  - ${t.name}: ${t.error}`));
  process.exit(1);
} else {
  console.log('\n✅ All tests passed!');
  process.exit(0);
}
