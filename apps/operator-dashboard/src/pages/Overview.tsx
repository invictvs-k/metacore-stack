import { useEffect, useState } from 'react';
import { Activity, Server, Terminal, CheckCircle, XCircle } from 'lucide-react';
import { useConfig } from '../hooks/useConfig';

export default function Overview() {
  const { config, isLoading } = useConfig();
  const [mcpStatus, setMcpStatus] = useState<any>(null);

  useEffect(() => {
    // Fetch MCP status
    fetch('/api/mcp/status')
      .then(res => res.json())
      .then(data => setMcpStatus(data))
      .catch(console.error);
  }, []);

  if (isLoading) {
    return <div className="text-gray-600 dark:text-gray-400">Loading...</div>;
  }

  return (
    <div>
      <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-8">
        System Overview
      </h1>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        {/* RoomServer Status */}
        <ServiceCard
          title="RoomServer"
          icon={<Server size={24} />}
          url={config?.roomServer.baseUrl}
          status="running"
        />

        {/* RoomOperator Status */}
        <ServiceCard
          title="RoomOperator"
          icon={<Terminal size={24} />}
          url={config?.roomOperator.baseUrl}
          status="running"
        />

        {/* MCP Status */}
        <ServiceCard
          title="MCP Providers"
          icon={<Activity size={24} />}
          status={mcpStatus?.connected ? 'connected' : 'disconnected'}
          info={`${mcpStatus?.providers?.length || 0} providers`}
        />
      </div>

      {/* Quick Stats */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
          Quick Stats
        </h2>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <StatItem label="Active Connections" value="0" />
          <StatItem label="Total Events" value="0" />
          <StatItem label="Tests Run" value="0" />
          <StatItem label="Commands Executed" value="0" />
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
}

function ServiceCard({ title, icon, url, status, info }: ServiceCardProps) {
  const isRunning = status === 'running' || status === 'connected';

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
        {isRunning ? (
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
        <div className={`w-2 h-2 rounded-full ${isRunning ? 'bg-green-500' : 'bg-red-500'}`} />
        <span className="text-sm text-gray-600 dark:text-gray-400 capitalize">
          {status}
        </span>
      </div>
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
