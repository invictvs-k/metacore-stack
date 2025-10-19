/**
 * Configuration for MCP Integration Tests
 */

export default {
  // RoomOperator configuration
  operator: {
    baseUrl: process.env.OPERATOR_URL || 'http://localhost:40802',
    timeout: 30000,
  },

  // RoomServer configuration
  roomServer: {
    baseUrl: process.env.ROOMSERVER_URL || 'http://localhost:40801',
    timeout: 10000,
  },

  // Test room configuration
  testRoom: {
    roomId: process.env.TEST_ROOM_ID || 'room-mcp-integration-test',
    specVersion: 1,
  },

  // MCP provider configurations
  mcp: {
    providers: [
      {
        id: 'example-mcp',
        url: 'ws://127.0.0.1:5099',
        visibility: 'room',
      },
      {
        id: 'test-mcp-unavailable',
        url: 'ws://127.0.0.1:5999',
        visibility: 'room',
      },
    ],
  },

  // Test scenarios to run
  scenarios: [
    '01-no-mcp',
    '02-load-mcp',
    '03-mcp-unavailable',
    '04-status',
  ],

  // Test execution settings
  timeouts: {
    readinessMs: 60000,
    scenarioMs: 120000,
    betweenScenariosMs: 2000,
  },

  // Output configuration
  output: {
    logsDir: process.env.ARTIFACTS_DIR || '.artifacts/integration',
    resultsFile: 'results/report.json',
    junitFile: 'results/junit.xml',
    traceFile: 'results/trace.ndjson',
  },

  // Execution settings
  execution: {
    verbose: process.env.VERBOSE === 'true' || false,
    failFast: process.env.FAIL_FAST === 'true' || false,
  },
};
