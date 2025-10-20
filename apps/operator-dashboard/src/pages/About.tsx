import { ExternalLink, Github, Book } from 'lucide-react';

export default function About() {
  return (
    <div>
      <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-8">
        About Operator Dashboard
      </h1>

      <div className="space-y-6">
        {/* Introduction */}
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">Overview</h2>
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            The Operator Dashboard is a comprehensive control and observability platform for
            managing RoomServer, RoomOperator, and Test Client components of the Metacore Stack.
          </p>
          <p className="text-gray-600 dark:text-gray-400">
            It provides real-time event monitoring, test execution capabilities, command
            orchestration, and dynamic configuration management through an intuitive web interface.
          </p>
        </div>

        {/* Features */}
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">Key Features</h2>
          <ul className="space-y-2 text-gray-600 dark:text-gray-400">
            <li className="flex items-start gap-2">
              <span className="text-blue-600 dark:text-blue-400 mt-1">•</span>
              <span>
                <strong>Real-time Event Streaming:</strong> Monitor events from RoomServer and
                RoomOperator via Server-Sent Events (SSE)
              </span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-blue-600 dark:text-blue-400 mt-1">•</span>
              <span>
                <strong>Test Execution:</strong> Run integration tests with live log streaming and
                artifact collection
              </span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-blue-600 dark:text-blue-400 mt-1">•</span>
              <span>
                <strong>Command Orchestration:</strong> Execute RoomOperator commands with dynamic
                parameter validation
              </span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-blue-600 dark:text-blue-400 mt-1">•</span>
              <span>
                <strong>Configuration Management:</strong> Edit and persist dashboard settings with
                hot reload
              </span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-blue-600 dark:text-blue-400 mt-1">•</span>
              <span>
                <strong>System Overview:</strong> Health checks and status monitoring for all
                components
              </span>
            </li>
          </ul>
        </div>

        {/* Architecture */}
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">Architecture</h2>
          <div className="space-y-4">
            <div>
              <h3 className="font-medium text-gray-900 dark:text-white mb-2">
                Frontend (Vite + React + TypeScript)
              </h3>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                Built with modern React and TypeScript, using Vite for fast development. Styled with
                TailwindCSS for a lightweight, responsive interface.
              </p>
            </div>
            <div>
              <h3 className="font-medium text-gray-900 dark:text-white mb-2">
                Integration API (Express + TypeScript)
              </h3>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                RESTful API server that acts as a hub between the dashboard and backend services.
                Handles event streaming, test execution, and configuration persistence.
              </p>
            </div>
          </div>
        </div>

        {/* Component Ports */}
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Service Ports
          </h2>
          <table className="w-full text-sm">
            <thead className="border-b border-gray-200 dark:border-gray-700">
              <tr>
                <th className="text-left py-2 text-gray-900 dark:text-white">Component</th>
                <th className="text-left py-2 text-gray-900 dark:text-white">Port</th>
                <th className="text-left py-2 text-gray-900 dark:text-white">Function</th>
              </tr>
            </thead>
            <tbody className="text-gray-600 dark:text-gray-400">
              <tr className="border-b border-gray-100 dark:border-gray-800">
                <td className="py-2 font-mono">RoomServer</td>
                <td className="py-2 font-mono">40801</td>
                <td className="py-2">MCP event emitter and status</td>
              </tr>
              <tr className="border-b border-gray-100 dark:border-gray-800">
                <td className="py-2 font-mono">RoomOperator</td>
                <td className="py-2 font-mono">40802</td>
                <td className="py-2">Command executor</td>
              </tr>
              <tr className="border-b border-gray-100 dark:border-gray-800">
                <td className="py-2 font-mono">Integration API</td>
                <td className="py-2 font-mono">40901</td>
                <td className="py-2">API hub and proxy</td>
              </tr>
              <tr>
                <td className="py-2 font-mono">Dashboard</td>
                <td className="py-2 font-mono">5173</td>
                <td className="py-2">Web UI (Vite dev server)</td>
              </tr>
            </tbody>
          </table>
        </div>

        {/* Links */}
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">Resources</h2>
          <div className="space-y-3">
            <ExternalLinkItem icon={<Book size={20} />} href="/docs" label="Documentation" />
            <ExternalLinkItem
              icon={<Github size={20} />}
              href="https://github.com/invictvs-k/metacore-stack"
              label="GitHub Repository"
            />
          </div>
        </div>

        {/* Version Info */}
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Version Information
          </h2>
          <dl className="space-y-2 text-sm">
            <div className="flex justify-between">
              <dt className="text-gray-600 dark:text-gray-400">Dashboard Version:</dt>
              <dd className="text-gray-900 dark:text-white font-mono">1.0.0</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-gray-600 dark:text-gray-400">Integration API:</dt>
              <dd className="text-gray-900 dark:text-white font-mono">1.0.0</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-gray-600 dark:text-gray-400">Built with:</dt>
              <dd className="text-gray-900 dark:text-white font-mono">React 18 + Vite 5</dd>
            </div>
          </dl>
        </div>
      </div>
    </div>
  );
}

interface ExternalLinkItemProps {
  icon: React.ReactNode;
  href: string;
  label: string;
}

function ExternalLinkItem({ icon, href, label }: ExternalLinkItemProps) {
  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      className="flex items-center gap-3 text-blue-600 dark:text-blue-400 hover:underline"
    >
      {icon}
      <span>{label}</span>
      <ExternalLink size={16} />
    </a>
  );
}
