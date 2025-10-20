#!/usr/bin/env node
/**
 * Contract tests - validate examples against schemas
 * Uses the existing schema validation system to ensure consistency
 */

import { exec } from 'child_process';
import { promisify } from 'util';

const execAsync = promisify(exec);

async function testSchemaValidation() {
  console.log('🔍 Running schema validation...');
  
  try {
    const { stdout } = await execAsync('npm run test:schemas', {
      cwd: process.cwd()
    });
    console.log(stdout);
    return true;
  } catch (error) {
    console.error('Schema validation failed:', error.stdout || error.message);
    return false;
  }
}

async function testContractValidation() {
  console.log('🔍 Running contract validation...');
  
  try {
    const { stdout } = await execAsync('npm run test:contracts', {
      cwd: process.cwd()
    });
    console.log(stdout);
    return true;
  } catch (error) {
    console.error('Contract validation failed:', error.stdout || error.message);
    return false;
  }
}

async function main() {
  console.log('🚀 Running contract tests...\n');
  
  const schemaOk = await testSchemaValidation();
  console.log('');
  
  const contractOk = await testContractValidation();
  console.log('');
  
  if (!schemaOk || !contractOk) {
    console.error('❌ Contract tests failed');
    process.exit(1);
  }
  
  console.log('✅ Contract tests passed');
}

main().catch(error => {
  console.error('❌ Contract test error:', error);
  process.exit(1);
});
