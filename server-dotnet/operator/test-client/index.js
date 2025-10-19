#!/usr/bin/env node

/**
 * RoomOperator Test Client - Main Entry Point
 * 
 * This client tests the integration between RoomOperator and RoomServer
 * by executing various scenarios and validating responses.
 */

import config from './config.js';
import Logger from './utils/logger.js';
import HttpClient from './utils/http-client.js';
import MessageBuilder from './utils/message-builder.js';

// Initialize logger
const logger = new Logger(config.execution.verbose);

// Display banner
logger.section('RoomOperator-RoomServer Test Client');
logger.info('Configuration loaded', {
  operatorUrl: config.operator.baseUrl,
  roomServerUrl: config.roomServer.baseUrl,
  testRoomId: config.testRoom.roomId,
});

// Export for use in scenarios
export { config, logger, HttpClient, MessageBuilder };

// If run directly, show usage
if (import.meta.url === `file://${process.argv[1]}`) {
  console.log(`
Usage:
  npm run test:basic    - Run basic flow scenario
  npm run test:error    - Run error cases scenario
  npm run test:stress   - Run stress test scenario
  npm run test:all      - Run all scenarios

Environment Variables:
  OPERATOR_URL          - RoomOperator base URL (default: http://localhost:8080)
  ROOMSERVER_URL        - RoomServer base URL (default: http://localhost:5000)
  ROOM_AUTH_TOKEN       - Authentication token for RoomServer
  TEST_ROOM_ID          - Test room ID (default: room-test-integration)
  VERBOSE               - Enable verbose logging (default: false)

Examples:
  export OPERATOR_URL=http://localhost:8080
  export ROOMSERVER_URL=http://localhost:5000
  export ROOM_AUTH_TOKEN=your-token
  npm run test:basic
`);
  process.exit(0);
}
