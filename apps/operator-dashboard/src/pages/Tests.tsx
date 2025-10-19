import { useState, useCallback } from 'react';
import { Play, CheckCircle, XCircle, Clock } from 'lucide-react';
import { useTestRunner } from '../hooks/useTestRunner';
import { useSSE } from '../hooks/useSSE';
import { useConfig } from '../hooks/useConfig';

export default function Tests() {
  const { scenarios, running, currentRunId, runTest } = useTestRunner();
  const { config } = useConfig();
  const [selectedScenario, setSelectedScenario] = useState<string>('all');
  const [logs, setLogs] = useState<string[]>([]);
  const [testStatus, setTestStatus] = useState<string | null>(null);
  const [exitCode, setExitCode] = useState<number | null>(null);
  const [artifactsDir, setArtifactsDir] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleLogMessage = useCallback((data: any) => {
    if (data.runId && data.chunk) {
      // Handle 'log' event
      setLogs(prev => [...prev, data.chunk]);
    } else if (data.runId && data.artifactsDir !== undefined) {
      // Handle 'started' event
      setArtifactsDir(data.artifactsDir);
    } else if (data.runId && data.exitCode !== undefined) {
      // Handle 'done' event
      setExitCode(data.exitCode);
      setTestStatus(data.exitCode === 0 ? 'completed' : 'failed');
    } else if (data.message) {
      // Handle 'error' event
      setLogs(prev => [...prev, `Error: ${data.message}`]);
      setTestStatus('failed');
    }
  }, []);

  const sseOptions = {
    reconnectInterval: config?.ui?.sseReconnectInterval || 5000,
    maxReconnectInterval: config?.ui?.sseMaxReconnectInterval || 30000,
    reconnectBackoffMultiplier: config?.ui?.sseReconnectBackoffMultiplier || 1.5
  };

  useSSE(
    currentRunId ? `/api/tests/stream/${currentRunId}` : '',
    handleLogMessage,
    !!currentRunId,
    sseOptions
  );

  const handleRunTest = async () => {
    setLogs([]);
    setTestStatus(null);
    setExitCode(null);
    setArtifactsDir(null);
    setError(null);
    try {
      await runTest(selectedScenario);
    } catch (error: any) {
      console.error('Failed to run test:', error);
      setError(error.message || 'Failed to start test execution');
      setTestStatus('failed');
    }
  };

  return (
    <div>
      <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-8">
        Test Scenarios
      </h1>

      {/* Test Selection */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6 mb-6">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
          Select Scenario
        </h2>
        <div className="flex gap-4 items-end">
          <div className="flex-1">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Test Scenario
            </label>
            <select
              value={selectedScenario}
              onChange={(e) => setSelectedScenario(e.target.value)}
              className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
              disabled={running}
            >
              <option value="all">All Tests</option>
              {scenarios.map((scenario) => (
                <option key={scenario.id} value={scenario.id}>
                  {scenario.name}
                </option>
              ))}
            </select>
          </div>
          <button
            onClick={handleRunTest}
            disabled={running}
            className={`
              px-6 py-2 rounded flex items-center gap-2 transition-colors
              ${running 
                ? 'bg-gray-400 cursor-not-allowed' 
                : 'bg-blue-600 hover:bg-blue-700'
              }
              text-white
            `}
          >
            <Play size={20} />
            {running ? 'Running...' : 'Run Test'}
          </button>
        </div>

        {/* Selected Scenario Info */}
        {selectedScenario !== 'all' && (
          <div className="mt-4 p-4 bg-gray-50 dark:bg-gray-900 rounded">
            {scenarios.find(s => s.id === selectedScenario)?.description || 'No description'}
          </div>
        )}

        {/* Error Message */}
        {error && (
          <div className="mt-4 p-4 bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200 rounded">
            <strong>Error:</strong> {error}
          </div>
        )}
      </div>

      {/* Test Results */}
      {currentRunId && (
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
              Test Results
            </h2>
            {testStatus && (
              <StatusBadge status={testStatus} />
            )}
          </div>

          {/* Logs */}
          <div className="bg-gray-900 text-green-400 p-4 rounded font-mono text-sm overflow-auto max-h-96">
            {logs.length === 0 ? (
              <div className="text-gray-500">Waiting for logs...</div>
            ) : (
              logs.map((log, index) => (
                <div key={index}>{log}</div>
              ))
            )}
          </div>

          {/* Run Info */}
          <div className="mt-4 text-sm text-gray-600 dark:text-gray-400 space-y-2">
            <div>
              Run ID: <code className="bg-gray-100 dark:bg-gray-900 px-2 py-1 rounded">{currentRunId}</code>
            </div>
            {artifactsDir && (
              <div>
                Artifacts: <code className="bg-gray-100 dark:bg-gray-900 px-2 py-1 rounded">{artifactsDir}</code>
              </div>
            )}
            {exitCode !== null && (
              <div>
                Exit Code: <code className={`px-2 py-1 rounded ${exitCode === 0 ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' : 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200'}`}>{exitCode}</code>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Available Scenarios List */}
      <div className="mt-6 bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
          Available Scenarios
        </h2>
        <div className="space-y-2">
          {scenarios.length === 0 ? (
            <p className="text-gray-600 dark:text-gray-400">No scenarios found</p>
          ) : (
            scenarios.map((scenario) => (
              <div
                key={scenario.id}
                className="p-4 border border-gray-200 dark:border-gray-700 rounded hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
              >
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="font-medium text-gray-900 dark:text-white">
                      {scenario.name}
                    </h3>
                    <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                      {scenario.description}
                    </p>
                  </div>
                  <code className="text-xs bg-gray-100 dark:bg-gray-900 px-2 py-1 rounded">
                    {scenario.script}
                  </code>
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}

interface StatusBadgeProps {
  status: string;
}

function StatusBadge({ status }: StatusBadgeProps) {
  if (status === 'completed') {
    return (
      <div className="flex items-center gap-2 text-green-600 dark:text-green-400">
        <CheckCircle size={20} />
        <span>Completed</span>
      </div>
    );
  }

  if (status === 'failed') {
    return (
      <div className="flex items-center gap-2 text-red-600 dark:text-red-400">
        <XCircle size={20} />
        <span>Failed</span>
      </div>
    );
  }

  return (
    <div className="flex items-center gap-2 text-blue-600 dark:text-blue-400">
      <Clock size={20} />
      <span>Running</span>
    </div>
  );
}
