export interface DashboardSettings {
  version: number;
  roomServer: {
    baseUrl: string;
    events: {
      type: string;
      path: string;
    };
  };
  roomOperator: {
    baseUrl: string;
    events: {
      type: string;
      path: string;
    };
  };
  testClient: {
    runner: string;
    scenariosPath: string;
    artifactsDir: string;
  };
  integrationApi: {
    port: number;
    logLevel: string;
  };
  ui: {
    theme: string;
    refreshInterval?: number;
    sseReconnectInterval?: number;
  };
}

export interface TestScenario {
  id: string;
  name: string;
  description: string;
  script: string;
  path: string;
}

export interface TestRun {
  runId: string;
  scenarioId: string;
  startTime: string;
  endTime?: string;
  status: 'running' | 'completed' | 'failed';
  exitCode?: number;
  artifactsPath: string;
}

export interface Command {
  id: string;
  title: string;
  description: string;
  paramsSchema: any;
  usage?: string;
}

export interface CommandCatalog {
  version: number;
  commands: Command[];
}

export interface EventMessage {
  source: 'roomserver' | 'roomoperator' | 'combined';
  timestamp: string;
  type: string;
  data: any;
}
