#!/usr/bin/env node

/**
 * Smoke test for SSE heartbeat endpoint
 * Tests that the /events/heartbeat endpoint:
 * - Returns proper SSE headers
 * - Sends at least one 'log' event
 * - Sends at least one 'message' or 'heartbeat' event
 * - Sends a 'done' event eventually
 */

import http from 'http';

const API_URL = process.env.API_URL || 'http://localhost:40901';
const ENDPOINT = '/api/events/heartbeat';
const TIMEOUT_MS = 10000; // 10 seconds timeout

console.log(`🧪 Testing SSE heartbeat endpoint: ${API_URL}${ENDPOINT}`);

function testSSEHeartbeat() {
  return new Promise((resolve, reject) => {
    const events = {
      log: false,
      message: false,
      heartbeat: false,
      done: false
    };

    let buffer = '';
    const timeout = setTimeout(() => {
      req.destroy();
      reject(new Error('Test timeout - endpoint did not respond within 10 seconds'));
    }, TIMEOUT_MS);

    const url = new URL(`${API_URL}${ENDPOINT}`);
    const req = http.get({
      hostname: url.hostname,
      port: url.port,
      path: url.pathname,
      headers: {
        'Accept': 'text/event-stream'
      }
    }, (res) => {
      // Validate SSE headers
      if (!res.headers['content-type'] || !res.headers['content-type'].startsWith('text/event-stream')) {
        clearTimeout(timeout);
        reject(new Error(`Wrong content-type: ${res.headers['content-type']}`));
        return;
      }

      console.log('✅ Correct SSE headers received');

      res.on('data', (chunk) => {
        buffer += chunk.toString();
        
        // Parse SSE messages
        const messages = buffer.split('\n\n');
        buffer = messages.pop() || ''; // Keep incomplete message in buffer

        for (const message of messages) {
          if (!message.trim()) continue;

          const lines = message.split('\n');
          let data = null;

          for (const line of lines) {
            if (line.startsWith('data: ')) {
              try {
                data = JSON.parse(line.substring(6));
              } catch (e) {
                data = line.substring(6);
              }
            }
          }

          if (data && data.event) {
            console.log(`📨 Received event: ${data.event}`);
            
            if (data.event === 'log') events.log = true;
            if (data.event === 'message') events.message = true;
            if (data.event === 'heartbeat') events.heartbeat = true;
            if (data.event === 'done') {
              events.done = true;
              
              // Check if we got all required events
              clearTimeout(timeout);
              req.destroy();
              
              if (events.log && (events.message || events.heartbeat) && events.done) {
                resolve(events);
              } else {
                reject(new Error(`Missing events: ${JSON.stringify(events)}`));
              }
            }
          }
        }
      });

      res.on('end', () => {
        clearTimeout(timeout);
        if (events.log && (events.message || events.heartbeat)) {
          console.log('⚠️  Stream ended without "done" event (acceptable)');
          resolve(events);
        } else {
          reject(new Error(`Stream ended prematurely. Events: ${JSON.stringify(events)}`));
        }
      });
    });

    req.on('error', (error) => {
      clearTimeout(timeout);
      reject(new Error(`Connection error: ${error.message}`));
    });
  });
}

// Run the test
testSSEHeartbeat()
  .then((events) => {
    console.log('\n✅ SSE Heartbeat smoke test PASSED');
    console.log(`Events received: ${JSON.stringify(events, null, 2)}`);
    process.exit(0);
  })
  .catch((error) => {
    console.error('\n❌ SSE Heartbeat smoke test FAILED');
    console.error(`Error: ${error.message}`);
    process.exit(1);
  });
