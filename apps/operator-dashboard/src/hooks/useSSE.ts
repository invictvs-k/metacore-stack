import { useEffect, useRef, useCallback } from 'react';

export function useSSE(url: string, onMessage: (data: any) => void, enabled: boolean = true) {
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
        
        // Reconnect after 5 seconds
        reconnectTimeoutRef.current = setTimeout(() => {
          connect();
        }, 5000);
      };

      eventSource.onopen = () => {
        console.log('SSE connection established:', url);
      };
    } catch (error) {
      console.error('Error creating EventSource:', error);
    }
  }, [url, onMessage, enabled]);

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
