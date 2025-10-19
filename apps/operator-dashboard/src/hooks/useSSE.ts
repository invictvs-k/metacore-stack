import { useEffect, useRef, useCallback } from 'react';

interface UseSSEOptions {
  reconnectInterval?: number;
  maxReconnectInterval?: number;
  reconnectBackoffMultiplier?: number;
}

export function useSSE(
  url: string, 
  onMessage: (data: any) => void, 
  enabled: boolean = true,
  options: UseSSEOptions = {}
) {
  const { 
    reconnectInterval = 5000,
    maxReconnectInterval = 30000,
    reconnectBackoffMultiplier = 1.5
  } = options;
  const eventSourceRef = useRef<EventSource | null>(null);
  const reconnectTimeoutRef = useRef<ReturnType<typeof setTimeout>>();
  const currentReconnectDelayRef = useRef<number>(reconnectInterval);
  const reconnectAttemptsRef = useRef<number>(0);

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
        
        // Increment reconnection attempts
        reconnectAttemptsRef.current += 1;
        
        // Calculate next delay with exponential backoff
        const nextDelay = Math.min(
          currentReconnectDelayRef.current * reconnectBackoffMultiplier,
          maxReconnectInterval
        );
        currentReconnectDelayRef.current = nextDelay;
        
        console.log(`Reconnecting in ${Math.round(nextDelay / 1000)}s (attempt ${reconnectAttemptsRef.current})...`);
        
        // Reconnect after calculated delay
        reconnectTimeoutRef.current = setTimeout(() => {
          connect();
        }, nextDelay);
      };

      eventSource.onopen = () => {
        console.log('SSE connection established:', url);
        // Reset backoff on successful connection
        currentReconnectDelayRef.current = reconnectInterval;
        reconnectAttemptsRef.current = 0;
      };
    } catch (error) {
      console.error('Error creating EventSource:', error);
    }
  }, [url, onMessage, enabled, reconnectInterval, maxReconnectInterval, reconnectBackoffMultiplier]);

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
      // Reset refs on cleanup
      currentReconnectDelayRef.current = reconnectInterval;
      reconnectAttemptsRef.current = 0;
    };
  }, [connect, reconnectInterval]);

  return {
    reconnect: connect
  };
}
