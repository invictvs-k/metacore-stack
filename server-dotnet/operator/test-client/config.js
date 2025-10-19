/**
 * Configuration for RoomOperator Test Client
 */

export default {
  // RoomOperator configuration
  operator: {
    baseUrl: process.env.OPERATOR_URL || 'http://localhost:8080',
    timeout: 30000,
  },

  // RoomServer configuration (for validation)
  roomServer: {
    baseUrl: process.env.ROOMSERVER_URL || 'http://localhost:5000',
    timeout: 10000,
  },

  // Authentication
  auth: {
    token: process.env.ROOM_AUTH_TOKEN || '',
  },

  // Test room configuration
  testRoom: {
    roomId: process.env.TEST_ROOM_ID || 'room-test-integration',
    specVersion: 1,
  },

  // Test entities
  entities: [
    {
      id: 'E-orchestrator-test',
      kind: 'orchestrator',
      displayName: 'Test Orchestrator',
      visibility: 'team',
      capabilities: ['execute_commands', 'manage_artifacts'],
      policy: {
        allow_commands_from: 'orchestrator',
        sandbox_mode: true,
        env_whitelist: [],
        scopes: [],
      },
    },
    {
      id: 'E-agent-test-1',
      kind: 'agent',
      displayName: 'Test Agent 1',
      visibility: 'team',
      capabilities: ['read_artifacts'],
      policy: {
        allow_commands_from: 'orchestrator',
        sandbox_mode: true,
        env_whitelist: [],
        scopes: [],
      },
    },
    {
      id: 'E-agent-test-2',
      kind: 'agent',
      displayName: 'Test Agent 2',
      visibility: 'public',
      capabilities: [],
      policy: {
        allow_commands_from: 'none',
        sandbox_mode: true,
        env_whitelist: [],
        scopes: [],
      },
    },
  ],

  // Test artifacts
  artifacts: [
    {
      name: 'test-document-1',
      type: 'document',
      workspace: 'shared',
      tags: ['test', 'integration'],
      content: 'This is a test document for integration testing.\n\nIt contains sample content to verify artifact seeding functionality.',
    },
    {
      name: 'test-config',
      type: 'config',
      workspace: 'shared',
      tags: ['test', 'config'],
      content: JSON.stringify({ testKey: 'testValue', enabled: true }, null, 2),
    },
  ],

  // Test policies
  policies: {
    dmVisibilityDefault: 'team',
    allowResourceCreation: false,
    maxArtifactsPerEntity: 100,
  },

  // Test execution settings
  execution: {
    delayBetweenOperations: 1000, // ms
    maxRetries: 3,
    retryDelay: 2000, // ms
    verbose: process.env.VERBOSE === 'true' || false,
  },
};
