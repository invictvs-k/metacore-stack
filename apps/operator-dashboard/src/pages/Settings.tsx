import { useState, useEffect, useRef } from 'react';
import { Save, RefreshCw } from 'lucide-react';
import { useConfig } from '../hooks/useConfig';

export default function Settings() {
  const { config, isLoading, updateConfig } = useConfig();
  const [editedConfig, setEditedConfig] = useState<string>('');
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const isInitialized = useRef(false);

  // Load config into editor when it becomes available
  useEffect(() => {
    if (config && !isInitialized.current) {
      setEditedConfig(JSON.stringify(config, null, 2));
      isInitialized.current = true;
    }
  }, [config]);

  const handleLoad = () => {
    if (config) {
      setEditedConfig(JSON.stringify(config, null, 2));
    }
  };

  const handleSave = async () => {
    setSaving(true);
    setMessage(null);

    try {
      const parsedConfig = JSON.parse(editedConfig);
      await updateConfig(parsedConfig);
      setMessage({ type: 'success', text: 'Configuration saved successfully!' });
    } catch (error: any) {
      setMessage({ type: 'error', text: `Error: ${error.message}` });
    } finally {
      setSaving(false);
    }
  };

  if (isLoading) {
    return <div className="text-gray-600 dark:text-gray-400">Loading configuration...</div>;
  }

  return (
    <div>
      <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-8">
        Settings
      </h1>

      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
            Dashboard Configuration
          </h2>
          <div className="flex gap-2">
            <button
              onClick={handleLoad}
              className="px-4 py-2 bg-gray-600 text-white rounded hover:bg-gray-700 transition-colors flex items-center gap-2"
            >
              <RefreshCw size={16} />
              Reload
            </button>
            <button
              onClick={handleSave}
              disabled={saving}
              className={`
                px-4 py-2 rounded flex items-center gap-2 transition-colors
                ${saving 
                  ? 'bg-gray-400 cursor-not-allowed' 
                  : 'bg-blue-600 hover:bg-blue-700'
                }
                text-white
              `}
            >
              <Save size={16} />
              {saving ? 'Saving...' : 'Save'}
            </button>
          </div>
        </div>

        {message && (
          <div className={`
            mb-4 p-4 rounded
            ${message.type === 'success' 
              ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' 
              : 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200'
            }
          `}>
            {message.text}
          </div>
        )}

        <div className="mb-4">
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Configuration (JSON)
          </label>
          <textarea
            value={editedConfig}
            onChange={(e) => setEditedConfig(e.target.value)}
            className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white font-mono text-sm"
            rows={20}
            spellCheck={false}
          />
        </div>

        <div className="bg-blue-50 dark:bg-blue-900/20 p-4 rounded">
          <h3 className="font-medium text-gray-900 dark:text-white mb-2">
            Configuration Guide
          </h3>
          <ul className="text-sm text-gray-600 dark:text-gray-400 space-y-1">
            <li><strong>roomServer.baseUrl:</strong> URL of the RoomServer instance</li>
            <li><strong>roomOperator.baseUrl:</strong> URL of the RoomOperator instance</li>
            <li><strong>testClient.runner:</strong> Path to test runner script</li>
            <li><strong>integrationApi.port:</strong> Port for the Integration API</li>
            <li><strong>ui.theme:</strong> UI theme (light, dark, or system)</li>
            <li><strong>ui.sseReconnectInterval:</strong> Initial SSE reconnection interval in ms (default: 5000)</li>
            <li><strong>ui.sseMaxReconnectInterval:</strong> Maximum SSE reconnection interval in ms (default: 30000)</li>
            <li><strong>ui.sseReconnectBackoffMultiplier:</strong> Exponential backoff multiplier (default: 1.5)</li>
          </ul>
        </div>
      </div>

      {/* Current Configuration Display */}
      <div className="mt-6 bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
          Current Configuration
        </h2>
        <dl className="space-y-3">
          <ConfigItem label="Version" value={config?.version} />
          <ConfigItem label="RoomServer" value={config?.roomServer.baseUrl} />
          <ConfigItem label="RoomOperator" value={config?.roomOperator.baseUrl} />
          <ConfigItem label="API Port" value={config?.integrationApi.port} />
          <ConfigItem label="Theme" value={config?.ui.theme} />
        </dl>
      </div>
    </div>
  );
}

interface ConfigItemProps {
  label: string;
  value: any;
}

function ConfigItem({ label, value }: ConfigItemProps) {
  return (
    <div className="flex justify-between items-center py-2 border-b border-gray-200 dark:border-gray-700">
      <dt className="text-sm font-medium text-gray-600 dark:text-gray-400">
        {label}
      </dt>
      <dd className="text-sm text-gray-900 dark:text-white font-mono">
        {typeof value === 'object' ? JSON.stringify(value) : String(value)}
      </dd>
    </div>
  );
}
