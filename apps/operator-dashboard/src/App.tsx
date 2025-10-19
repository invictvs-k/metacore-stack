import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { Home, Activity, TestTube, Terminal, Settings as SettingsIcon, Info } from 'lucide-react';
import Overview from './pages/Overview';
import Events from './pages/Events';
import Tests from './pages/Tests';
import Commands from './pages/Commands';
import Settings from './pages/Settings';
import About from './pages/About';

function App() {
  return (
    <BrowserRouter>
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
        <div className="flex">
          {/* Sidebar */}
          <aside className="w-64 bg-white dark:bg-gray-800 shadow-md min-h-screen">
            <div className="p-6">
              <h1 className="text-2xl font-bold text-gray-800 dark:text-white">
                Operator Dashboard
              </h1>
              <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Control & Observability
              </p>
            </div>
            
            <nav className="mt-6">
              <NavLink to="/" icon={<Home size={20} />}>
                Overview
              </NavLink>
              <NavLink to="/events" icon={<Activity size={20} />}>
                Events
              </NavLink>
              <NavLink to="/tests" icon={<TestTube size={20} />}>
                Tests
              </NavLink>
              <NavLink to="/commands" icon={<Terminal size={20} />}>
                Commands
              </NavLink>
              <NavLink to="/settings" icon={<SettingsIcon size={20} />}>
                Settings
              </NavLink>
              <NavLink to="/about" icon={<Info size={20} />}>
                About
              </NavLink>
            </nav>
          </aside>

          {/* Main content */}
          <main className="flex-1 p-8">
            <Routes>
              <Route path="/" element={<Overview />} />
              <Route path="/events" element={<Events />} />
              <Route path="/tests" element={<Tests />} />
              <Route path="/commands" element={<Commands />} />
              <Route path="/settings" element={<Settings />} />
              <Route path="/about" element={<About />} />
            </Routes>
          </main>
        </div>
      </div>
    </BrowserRouter>
  );
}

interface NavLinkProps {
  to: string;
  icon: React.ReactNode;
  children: React.ReactNode;
}

function NavLink({ to, icon, children }: NavLinkProps) {
  return (
    <Link
      to={to}
      className="flex items-center gap-3 px-6 py-3 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
    >
      {icon}
      <span>{children}</span>
    </Link>
  );
}

export default App;
