#!/usr/bin/env node
/**
 * Smoke tests - basic build and endpoint validation
 * Tests that essential services are buildable and respond to basic requests
 */

import { exec } from 'child_process';
import { promisify } from 'util';
import http from 'http';
import { join } from 'path';

const execAsync = promisify(exec);

// Helper to check if a URL responds
async function checkEndpoint(url, timeout = 5000) {
  return new Promise((resolve, reject) => {
    const timeoutId = setTimeout(() => {
      reject(new Error(`Timeout checking ${url}`));
    }, timeout);

    http.get(url, (res) => {
      clearTimeout(timeoutId);
      resolve({
        statusCode: res.statusCode,
        headers: res.headers
      });
    }).on('error', (err) => {
      clearTimeout(timeoutId);
      reject(err);
    });
  });
}

async function testBuild() {
  console.log('🔨 Testing builds...');
  
  try {
    // Test Node/TS build
    console.log('  - Building integration-api...');
    await execAsync('npm run build', {
      cwd: join(process.cwd(), 'tools', 'integration-api')
    });
    console.log('    ✅ integration-api builds successfully');
    
    // Test schema validation
    console.log('  - Validating schemas...');
    await execAsync('npm run test:schemas', {
      cwd: process.cwd()
    });
    console.log('    ✅ Schemas valid');
    
    return true;
  } catch (error) {
    console.error('❌ Build test failed:', error.message);
    return false;
  }
}

async function testEndpoints() {
  console.log('🌐 Testing endpoints (if services are running)...');
  
  const endpoints = [
    { url: 'http://localhost:40901/health', name: 'Integration API Health' },
    { url: 'http://localhost:40901/events/heartbeat', name: 'SSE Heartbeat' }
  ];
  
  for (const endpoint of endpoints) {
    try {
      const result = await checkEndpoint(endpoint.url, 3000);
      console.log(`  ✅ ${endpoint.name}: ${result.statusCode}`);
    } catch (error) {
      console.log(`  ⚠️  ${endpoint.name}: ${error.message} (service may not be running)`);
    }
  }
  
  return true;
}

async function main() {
  console.log('🚀 Running smoke tests...\n');
  
  const buildOk = await testBuild();
  console.log('');
  
  await testEndpoints();
  console.log('');
  
  if (!buildOk) {
    console.error('❌ Smoke tests failed');
    process.exit(1);
  }
  
  console.log('✅ Smoke tests passed');
}

main().catch(error => {
  console.error('❌ Smoke test error:', error);
  process.exit(1);
});
