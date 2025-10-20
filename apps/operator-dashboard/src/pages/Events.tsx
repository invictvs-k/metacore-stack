import { useState, useCallback, useRef, useEffect } from 'react';
import { Server, Terminal, Pause, Play } from 'lucide-react';
import { useSSE } from '../hooks/useSSE';
import { useAppStore } from '../store/useAppStore';
import { useConfig } from '../hooks/useConfig';

export default function Events() {
  const [filter, setFilter] = useState<'all' | 'roomserver' | 'roomoperator'>('all');
  const [paused, setPaused] = useState(false);
  const [autoScroll, setAutoScroll] = useState(true);
  const { events, addEvent, clearEvents } = useAppStore();
  const { config } = useConfig();
  const eventsEndRef = useRef<HTMLDivElement>(null);

  const handleMessage = useCallback((data: any) => {
    if (!paused) {
      addEvent({
        ...data,
        id: Date.now() + Math.random(),
        receivedAt: new Date().toISOString()
      });
    }
  }, [addEvent, paused]);

  const sseOptions = {
    reconnectInterval: config?.ui?.sseReconnectInterval || 5000,
    maxReconnectInterval: config?.ui?.sseMaxReconnectInterval || 30000,
    reconnectBackoffMultiplier: config?.ui?.sseReconnectBackoffMultiplier || 1.5
  };

  // Subscribe to combined events
  useSSE('/api/events/combined', handleMessage, filter === 'all' && !paused, sseOptions);
  useSSE('/api/events/roomserver', handleMessage, filter === 'roomserver' && !paused, sseOptions);
  useSSE('/api/events/roomoperator', handleMessage, filter === 'roomoperator' && !paused, sseOptions);

  // Auto-scroll to bottom
  useEffect(() => {
    if (autoScroll && eventsEndRef.current) {
      eventsEndRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [events, autoScroll]);

  const filteredEvents = filter === 'all' 
    ? events 
    : events.filter(e => e.source === filter);

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
          Real-time Events
        </h1>
        <div className="flex gap-2">
          <button
            onClick={() => setPaused(!paused)}
            className={`px-4 py-2 rounded flex items-center gap-2 transition-colors ${
              paused 
                ? 'bg-green-600 hover:bg-green-700' 
                : 'bg-yellow-600 hover:bg-yellow-700'
            } text-white`}
          >
            {paused ? <Play size={16} /> : <Pause size={16} />}
            {paused ? 'Resume' : 'Pause'}
          </button>
          <button
            onClick={() => setAutoScroll(!autoScroll)}
            className={`px-4 py-2 rounded transition-colors ${
              autoScroll 
                ? 'bg-blue-600 hover:bg-blue-700 text-white' 
                : 'bg-gray-300 hover:bg-gray-400 text-gray-700'
            }`}
          >
            Auto-scroll: {autoScroll ? 'On' : 'Off'}
          </button>
          <button
            onClick={clearEvents}
            className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 transition-colors"
          >
            Clear Events
          </button>
        </div>
      </div>

      {/* Filter Buttons */}
      <div className="flex gap-2 mb-6">
        <FilterButton
          active={filter === 'all'}
          onClick={() => setFilter('all')}
        >
          All Events
        </FilterButton>
        <FilterButton
          active={filter === 'roomserver'}
          onClick={() => setFilter('roomserver')}
          icon={<Server size={16} />}
        >
          RoomServer
        </FilterButton>
        <FilterButton
          active={filter === 'roomoperator'}
          onClick={() => setFilter('roomoperator')}
          icon={<Terminal size={16} />}
        >
          RoomOperator
        </FilterButton>
      </div>

      {/* Events Display */}
      <div className="grid grid-cols-1 gap-4">
        {filteredEvents.length === 0 ? (
          <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-8 text-center">
            <p className="text-gray-600 dark:text-gray-400">
              {paused ? 'Events paused. Click Resume to continue.' : 'No events yet. Events will appear here in real-time.'}
            </p>
          </div>
        ) : (
          <>
            {filteredEvents.slice().reverse().map((event) => (
              <EventCard key={event.id} event={event} />
            ))}
            <div ref={eventsEndRef} />
          </>
        )}
      </div>
    </div>
  );
}

interface FilterButtonProps {
  active: boolean;
  onClick: () => void;
  icon?: React.ReactNode;
  children: React.ReactNode;
}

function FilterButton({ active, onClick, icon, children }: FilterButtonProps) {
  return (
    <button
      onClick={onClick}
      className={`
        px-4 py-2 rounded flex items-center gap-2 transition-colors
        ${active 
          ? 'bg-blue-600 text-white' 
          : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
        }
      `}
    >
      {icon}
      {children}
    </button>
  );
}

interface EventCardProps {
  event: any;
}

function EventCard({ event }: EventCardProps) {
  const sourceColor = event.source === 'roomserver' 
    ? 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200'
    : 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200';

  // Extract event type - handle nested payload structure from RoomServer
  const eventType = event.payload?.kind || event.type;
  
  // Extract event data - handle nested payload structure
  const eventData = event.payload?.data || event.data || event.payload;

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4">
      <div className="flex items-start justify-between mb-2">
        <div className="flex items-center gap-2">
          <span className={`px-2 py-1 rounded text-xs font-medium ${sourceColor}`}>
            {event.source}
          </span>
          <span className="text-sm text-gray-600 dark:text-gray-400">
            {eventType}
          </span>
          {event.roomId && (
            <span className="text-xs text-gray-500 dark:text-gray-500">
              room: {event.roomId}
            </span>
          )}
        </div>
        <span className="text-xs text-gray-500 dark:text-gray-500">
          {new Date(event.ts || event.timestamp || event.receivedAt).toLocaleTimeString()}
        </span>
      </div>
      {eventData && (
        <pre className="text-sm bg-gray-50 dark:bg-gray-900 p-3 rounded overflow-x-auto">
          {JSON.stringify(eventData, null, 2)}
        </pre>
      )}
    </div>
  );
}
