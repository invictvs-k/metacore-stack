import { useEffect, useRef, useCallback } from 'react';

interface UseSSEOptions {
  reconnectInterval?: number;
}

export function useSSE(
  url: string, 
  onMessage: (data: any) => void, 
  enabled: boolean = true,
  options: UseSSEOptions = {}
) {
  const { reconnectInterval = 5000 } = options;
  const eventSourceRef = useRef<EventSource | null>(null);
  const reconnectTimeoutRef = useRef<ReturnType<typeof setTimeout>>();

  const connect = useCallback(() => {
    if (!enabled || !url) return;

    try {
      const eventSource = new EventSource(url);
      eventSourceRef.current = eventSource;

      eventSource.onmessage = (event) => {
        try {
          const data = JSON.parse(event.data);
          onMessage(data);
        } catch (error) {
          console.error('Error parsing SSE message:', error);
        }
      };

      eventSource.onerror = () => {
        console.error('SSE connection error, reconnecting...');
        eventSource.close();
        eventSourceRef.current = null;
        
        // Reconnect after configured interval
        reconnectTimeoutRef.current = setTimeout(() => {
          connect();
        }, reconnectInterval);
      };

      eventSource.onopen = () => {
        console.log('SSE connection established:', url);
      };
    } catch (error) {
      console.error('Error creating EventSource:', error);
    }
  }, [url, onMessage, enabled, reconnectInterval]);

  useEffect(() => {
    connect();

    return () => {
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
      }
      if (eventSourceRef.current) {
        eventSourceRef.current.close();
        eventSourceRef.current = null;
      }
    };
  }, [connect]);

  return {
    reconnect: connect
  };
}
