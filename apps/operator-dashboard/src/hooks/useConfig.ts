import useSWR from 'swr';
import type { DashboardSettings } from '../types';

const fetcher = (url: string) => fetch(url).then((res) => res.json());

export function useConfig() {
  const { data, error, mutate } = useSWR<DashboardSettings>('/api/config', fetcher, {
    refreshInterval: 10000, // Refresh every 10 seconds
  });

  const updateConfig = async (newConfig: DashboardSettings) => {
    try {
      const response = await fetch('/api/config', {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(newConfig),
      });

      if (!response.ok) {
        throw new Error('Failed to update config');
      }

      const result = await response.json();
      mutate(result.config);
      return result;
    } catch (error) {
      console.error('Error updating config:', error);
      throw error;
    }
  };

  return {
    config: data,
    isLoading: !error && !data,
    isError: error,
    updateConfig,
    refresh: mutate,
  };
}
