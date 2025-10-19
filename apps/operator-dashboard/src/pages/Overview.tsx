import { useEffect, useState } from 'react';
import { Activity, Server, Terminal, CheckCircle, XCircle, RefreshCw, Play, Trash2 } from 'lucide-react';
import { useConfig } from '../hooks/useConfig';
import { useAppStore } from '../store/useAppStore';

// Timeout for health check requests in milliseconds
const HEALTH_CHECK_TIMEOUT_MS = 5000;

export default function Overview() {
  const { config, isLoading } = useConfig();
  const { events } = useAppStore();
  const [mcpStatus, setMcpStatus] = useState<any>(null);
  const [healthStatus, setHealthStatus] = useState<any>({
    roomServer: { status: 'checking', error: null },
    roomOperator: { status: 'checking', error: null }
  });
  const [refreshing, setRefreshing] = useState(false);

  const checkHealth = async () => {
    setRefreshing(true);
    const newStatus: any = {
      roomServer: { status: 'checking', error: null },
      roomOperator: { status: 'checking', error: null }
    };

    // Check RoomServer via Integration API proxy
    try {
      const response = await fetch('/api/health/roomserver', {
        signal: AbortSignal.timeout(HEALTH_CHECK_TIMEOUT_MS)
      });
      
      if (response.ok) {
        const data = await response.json();
        newStatus.roomServer = {
          status: 'healthy',
          error: null
        };
      } else {
        const data = await response.json();
        newStatus.roomServer = {
          status: 'error',
          error: data.error || `HTTP ${response.status}`
        };
      }
    } catch (error: any) {
      newStatus.roomServer = {
        status: 'error',
        error: error.name === 'TimeoutError' ? 'Connection timeout' : (error.message || 'Connection failed')
      };
    }

    // Check RoomOperator via Integration API proxy
    try {
      const response = await fetch('/api/health/roomoperator', {
        signal: AbortSignal.timeout(HEALTH_CHECK_TIMEOUT_MS)
      });
      
      if (response.ok) {
        const data = await response.json();
        newStatus.roomOperator = {
          status: 'healthy',
          error: null
        };
      } else {
        const data = await response.json();
        newStatus.roomOperator = {
          status: 'error',
          error: data.error || `HTTP ${response.status}`
        };
      }
    } catch (error: any) {
      newStatus.roomOperator = {
        status: 'error',
        error: error.name === 'TimeoutError' ? 'Connection timeout' : (error.message || 'Connection failed')
      };
    }

    setHealthStatus(newStatus);
    setRefreshing(false);
  };

  useEffect(() => {
    if (config) {
      checkHealth();
      
      // Fetch MCP status
      fetch('/api/mcp/status')
        .then(res => res.json())
        .then(data => setMcpStatus(data))
        .catch(console.error);

      // Poll every 10 seconds
      const interval = setInterval(() => {
        checkHealth();
        fetch('/api/mcp/status')
          .then(res => res.json())
          .then(data => setMcpStatus(data))
          .catch(console.error);
      }, 10000);

      return () => clearInterval(interval);
    }
  }, [config]);

  const handleRunAll = async () => {
    try {
      await fetch('/api/tests/run', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ all: true })
      });
    } catch (error) {
      console.error('Failed to run all tests:', error);
    }
  };

  if (isLoading) {
    return <div className="text-gray-600 dark:text-gray-400">Loading...</div>;
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-8">
        <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
          System Overview
        </h1>
        <button
          onClick={checkHealth}
          disabled={refreshing}
          className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors flex items-center gap-2"
        >
          <RefreshCw size={16} className={refreshing ? 'animate-spin' : ''} />
          Refresh
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        {/* RoomServer Status */}
        <ServiceCard
          title="RoomServer"
          icon={<Server size={24} />}
          url={config?.roomServer.baseUrl}
          status={healthStatus.roomServer.status}
          error={healthStatus.roomServer.error}
        />

        {/* RoomOperator Status */}
        <ServiceCard
          title="RoomOperator"
          icon={<Terminal size={24} />}
          url={config?.roomOperator.baseUrl}
          status={healthStatus.roomOperator.status}
          error={healthStatus.roomOperator.error}
        />

        {/* MCP Status */}
        <ServiceCard
          title="MCP Providers"
          icon={<Activity size={24} />}
          status={mcpStatus?.connected ? 'healthy' : 'disconnected'}
          info={`${mcpStatus?.providers?.length || 0} providers`}
        />
      </div>

      {/* Quick Actions */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6 mb-8">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
          Quick Actions
        </h2>
        <div className="flex gap-4">
          <button
            onClick={handleRunAll}
            className="px-6 py-3 bg-green-600 text-white rounded hover:bg-green-700 transition-colors flex items-center gap-2"
          >
            <Play size={20} />
            Run All Tests
          </button>
          <button
            className="px-6 py-3 bg-orange-600 text-white rounded hover:bg-orange-700 transition-colors flex items-center gap-2"
            onClick={() => {
              // TODO: Implement clean artifacts
              console.log('Clean artifacts not yet implemented');
            }}
          >
            <Trash2 size={20} />
            Clean Artifacts
          </button>
        </div>
      </div>

      {/* Quick Stats */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
          Quick Stats
        </h2>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <StatItem label="Events Received" value={events.length.toString()} />
          <StatItem label="Services Healthy" value={`${Object.values(healthStatus).filter((s: any) => s.status === 'healthy').length}/2`} />
          <StatItem label="MCP Providers" value={mcpStatus?.providers?.length?.toString() || '0'} />
          <StatItem label="Integration API" value="Running" />
        </div>
      </div>
    </div>
  );
}

interface ServiceCardProps {
  title: string;
  icon: React.ReactNode;
  url?: string;
  status: string;
  info?: string;
  error?: string | null;
}

function ServiceCard({ title, icon, url, status, info, error }: ServiceCardProps) {
  const isHealthy = status === 'healthy' || status === 'connected';
  const isChecking = status === 'checking';

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-3">
          <div className="text-blue-600 dark:text-blue-400">
            {icon}
          </div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
            {title}
          </h3>
        </div>
        {isChecking ? (
          <RefreshCw className="text-gray-500 animate-spin" size={20} />
        ) : isHealthy ? (
          <CheckCircle className="text-green-500" size={20} />
        ) : (
          <XCircle className="text-red-500" size={20} />
        )}
      </div>
      {url && (
        <p className="text-sm text-gray-600 dark:text-gray-400 mb-2">
          {url}
        </p>
      )}
      <div className="flex items-center gap-2">
        <div className={`w-2 h-2 rounded-full ${isHealthy ? 'bg-green-500' : isChecking ? 'bg-yellow-500' : 'bg-red-500'}`} />
        <span className="text-sm text-gray-600 dark:text-gray-400 capitalize">
          {status}
        </span>
      </div>
      {error && (
        <p className="text-xs text-red-500 dark:text-red-400 mt-2">
          {error}
        </p>
      )}
      {info && (
        <p className="text-sm text-gray-500 dark:text-gray-500 mt-2">
          {info}
        </p>
      )}
    </div>
  );
}

interface StatItemProps {
  label: string;
  value: string;
}

function StatItem({ label, value }: StatItemProps) {
  return (
    <div>
      <p className="text-2xl font-bold text-gray-900 dark:text-white">
        {value}
      </p>
      <p className="text-sm text-gray-600 dark:text-gray-400">
        {label}
      </p>
    </div>
  );
}
