import { useState, useEffect } from 'react';
import { Send, CheckCircle, XCircle } from 'lucide-react';
import type { CommandCatalog } from '../types';

export default function Commands() {
  const [catalog, setCatalog] = useState<CommandCatalog | null>(null);
  const [selectedCommand, setSelectedCommand] = useState<string>('');
  const [params, setParams] = useState<string>('{}');
  const [executing, setExecuting] = useState(false);
  const [result, setResult] = useState<any>(null);

  useEffect(() => {
    fetch('/api/commands')
      .then((res) => res.json())
      .then((data) => setCatalog(data))
      .catch(console.error);
  }, []);

  const handleExecute = async () => {
    if (!selectedCommand) return;

    setExecuting(true);
    setResult(null);

    try {
      const parsedParams = JSON.parse(params);
      const response = await fetch('/api/commands/execute', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          commandId: selectedCommand,
          params: parsedParams,
        }),
      });

      const data = await response.json();
      setResult(data);
    } catch (error: any) {
      setResult({ error: error.message });
    } finally {
      setExecuting(false);
    }
  };

  const selectedCommandInfo = catalog?.commands.find((c) => c.id === selectedCommand);

  return (
    <div>
      <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-8">Command Execution</h1>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Command Selection & Execution */}
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Execute Command
          </h2>

          {/* Command Select */}
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Command
            </label>
            <select
              value={selectedCommand}
              onChange={(e) => setSelectedCommand(e.target.value)}
              className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
              disabled={executing}
            >
              <option value="">Select a command...</option>
              {catalog?.commands.map((cmd) => (
                <option key={cmd.id} value={cmd.id}>
                  {cmd.title}
                </option>
              ))}
            </select>
          </div>

          {/* Command Info */}
          {selectedCommandInfo && (
            <div className="mb-4 p-4 bg-blue-50 dark:bg-blue-900/20 rounded">
              <h3 className="font-medium text-gray-900 dark:text-white mb-2">
                {selectedCommandInfo.title}
              </h3>
              <p className="text-sm text-gray-600 dark:text-gray-400 mb-2">
                {selectedCommandInfo.description}
              </p>
              {selectedCommandInfo.usage && (
                <p className="text-xs text-gray-500 dark:text-gray-500">
                  Usage: {selectedCommandInfo.usage}
                </p>
              )}
            </div>
          )}

          {/* Parameters */}
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Parameters (JSON)
            </label>
            <textarea
              value={params}
              onChange={(e) => setParams(e.target.value)}
              className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white font-mono text-sm"
              rows={6}
              disabled={executing}
              placeholder="{}"
            />
          </div>

          {/* Execute Button */}
          <button
            onClick={handleExecute}
            disabled={!selectedCommand || executing}
            className={`
              w-full px-6 py-3 rounded flex items-center justify-center gap-2 transition-colors
              ${
                !selectedCommand || executing
                  ? 'bg-gray-400 cursor-not-allowed'
                  : 'bg-blue-600 hover:bg-blue-700'
              }
              text-white font-medium
            `}
          >
            <Send size={20} />
            {executing ? 'Executing...' : 'Execute Command'}
          </button>

          {/* Result */}
          {result && (
            <div className="mt-4">
              <div className="flex items-center gap-2 mb-2">
                {result.success ? (
                  <CheckCircle className="text-green-500" size={20} />
                ) : (
                  <XCircle className="text-red-500" size={20} />
                )}
                <span className="font-medium text-gray-900 dark:text-white">
                  {result.success ? 'Success' : 'Error'}
                </span>
              </div>
              <pre className="text-sm bg-gray-900 text-green-400 p-4 rounded overflow-x-auto">
                {JSON.stringify(result, null, 2)}
              </pre>
            </div>
          )}
        </div>

        {/* Command Catalog */}
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Available Commands
          </h2>

          <div className="space-y-4">
            {catalog?.commands.map((cmd) => (
              <div
                key={cmd.id}
                className={`
                  p-4 border rounded cursor-pointer transition-colors
                  ${
                    selectedCommand === cmd.id
                      ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20'
                      : 'border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700'
                  }
                `}
                onClick={() => setSelectedCommand(cmd.id)}
              >
                <h3 className="font-medium text-gray-900 dark:text-white mb-1">{cmd.title}</h3>
                <p className="text-sm text-gray-600 dark:text-gray-400 mb-2">{cmd.description}</p>
                <code className="text-xs bg-gray-100 dark:bg-gray-900 px-2 py-1 rounded">
                  {cmd.id}
                </code>
              </div>
            ))}
          </div>

          {!catalog && <p className="text-gray-600 dark:text-gray-400">Loading commands...</p>}
        </div>
      </div>
    </div>
  );
}
