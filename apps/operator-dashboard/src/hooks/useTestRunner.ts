import { useState, useCallback } from 'react';
import useSWR from 'swr';
import type { TestScenario } from '../types';

const fetcher = (url: string) => fetch(url).then((res) => res.json());

export function useTestRunner() {
  const [running, setRunning] = useState(false);
  const [currentRunId, setCurrentRunId] = useState<string | null>(null);

  const { data: scenariosData } = useSWR<{ scenarios: TestScenario[] }>(
    '/api/tests',
    fetcher
  );

  const runTest = useCallback(async (scenarioId: string = 'all') => {
    setRunning(true);
    try {
      const body = scenarioId === 'all' 
        ? { all: true }
        : { scenarioId };
        
      const response = await fetch('/api/tests/run', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(body),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.error || 'Failed to start test');
      }

      const result = await response.json();
      setCurrentRunId(result.runId);
      return result.runId;
    } catch (error) {
      console.error('Error running test:', error);
      setRunning(false);
      throw error;
    } finally {
      setRunning(false);
    }
  }, []);

  const getRunMetadata = useCallback(async (runId: string) => {
    try {
      const response = await fetch(`/api/tests/runs/${runId}`);
      if (!response.ok) {
        throw new Error('Failed to get run metadata');
      }
      return await response.json();
    } catch (error) {
      console.error('Error getting run metadata:', error);
      throw error;
    }
  }, []);

  return {
    scenarios: scenariosData?.scenarios || [],
    running,
    currentRunId,
    runTest,
    getRunMetadata,
  };
}
