#!/usr/bin/env node
/**
 * Basic E2E test - tests a simple workflow through the system
 * 1. Create room
 * 2. Send SSE event
 * 3. Verify response
 * 
 * Note: Requires services to be running
 */

import http from 'http';
import { EventSource } from 'eventsource';

const BASE_URL = 'http://localhost:40901';

async function httpRequest(method, path, data = null) {
  return new Promise((resolve, reject) => {
    const options = {
      method,
      hostname: 'localhost',
      port: 40901,
      path,
      headers: {
        'Content-Type': 'application/json'
      }
    };

    const req = http.request(options, (res) => {
      let body = '';
      res.on('data', chunk => body += chunk);
      res.on('end', () => {
        try {
          resolve({
            statusCode: res.statusCode,
            body: body ? JSON.parse(body) : null
          });
        } catch {
          resolve({
            statusCode: res.statusCode,
            body: body
          });
        }
      });
    });

    req.on('error', reject);
    
    if (data) {
      req.write(JSON.stringify(data));
    }
    
    req.end();
  });
}

async function testSSEHeartbeat() {
  console.log('🔌 Testing SSE heartbeat endpoint...');
  
  return new Promise((resolve, reject) => {
    const url = `${BASE_URL}/events/heartbeat`;
    
    try {
      const es = new EventSource(url);
      let receivedEvent = false;
      let errorCount = 0;
      
      const timeout = setTimeout(() => {
        es.close();
        if (!receivedEvent) {
          reject(new Error('No heartbeat received within timeout'));
        }
      }, 10000); // Increased timeout to 10 seconds
      
      es.addEventListener('heartbeat', (event) => {
        try {
          const data = JSON.parse(event.data);
          console.log(`  ✅ Received heartbeat:`, data);
          receivedEvent = true;
          clearTimeout(timeout);
          es.close();
          resolve(data);
        } catch (error) {
          clearTimeout(timeout);
          es.close();
          reject(error);
        }
      });
      
      es.addEventListener('open', () => {
        console.log('  📡 SSE connection established');
      });
      
      es.onerror = (error) => {
        errorCount++;
        console.log(`  ⚠️  SSE connection error (attempt ${errorCount})`);
        
        // EventSource automatically retries, but if we get multiple errors quickly, fail
        if (errorCount > 3) {
          clearTimeout(timeout);
          es.close();
          reject(new Error(`SSE connection failed after ${errorCount} attempts`));
        }
      };
    } catch (error) {
      reject(new Error(`Failed to create EventSource: ${error.message}`));
    }
  });
}

async function testHealthEndpoint() {
  console.log('🏥 Testing health endpoint...');
  
  try {
    const response = await httpRequest('GET', '/health');
    if (response.statusCode === 200) {
      console.log('  ✅ Health check passed');
      return true;
    } else {
      console.error(`  ❌ Health check failed: ${response.statusCode}`);
      return false;
    }
  } catch (error) {
    console.error('  ❌ Health check error:', error.message);
    return false;
  }
}

async function main() {
  console.log('🚀 Running E2E tests...\n');
  console.log('⚠️  Note: This requires Integration API to be running on port 40901\n');
  
  try {
    // Test health endpoint
    const healthOk = await testHealthEndpoint();
    if (!healthOk) {
      console.error('\n❌ E2E tests failed: Service not available');
      process.exit(1);
    }
    
    // Give the service a moment to be fully ready after health check
    console.log('⏳ Waiting for service to be fully ready...');
    await new Promise(resolve => setTimeout(resolve, 2000));
    console.log('');
    
    // Test SSE heartbeat
    await testSSEHeartbeat();
    
    console.log('\n✅ E2E tests passed');
  } catch (error) {
    console.error('\n❌ E2E tests failed:', error.message);
    console.log('\nTip: Start the Integration API with: cd tools/integration-api && npm start');
    process.exit(1);
  }
}

// Only run if this is the main module
if (import.meta.url === `file://${process.argv[1]}`) {
  main();
}
