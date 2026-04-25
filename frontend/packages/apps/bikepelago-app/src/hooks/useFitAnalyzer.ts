import { useState } from 'react';

import { API_BASE } from '../lib/api';
import { getToken } from '../store/authStore';
import { useGameStore } from '../store/gameStore';
import type { FitAnalysisResult } from '../types/game';

export function useFitAnalyzer(
  sessionId: string,
  onAnalysisComplete: (result: FitAnalysisResult | null) => void,
) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const analyzeFile = async (selectedFile: File | null) => {
    if (!selectedFile) return;

    setLoading(true);
    setError(null);
    onAnalysisComplete(null);

    console.log(
      '[useFitAnalyzer] Sending analysis to backend for:',
      selectedFile.name,
    );

    if ((window as unknown as { PLAYWRIGHT_TEST?: boolean }).PLAYWRIGHT_TEST) {
      console.log('[useFitAnalyzer] Mocking analysis for E2E test');
      onAnalysisComplete({
        path: [{ lat: 40.7128, lon: -74.006 }],
        stats: {
          distanceMeters: 5000,
          durationSeconds: 1200,
          elevationGainMeters: 100,
          avgSpeedKph: 15,
        },
        newlyCheckedNodes: [
          {
            id: 'mock_node_1',
            apArrivalLocationId: 1001,
            apPrecisionLocationId: 1002,
            arrivalChecked: true,
            precisionChecked: true,
            lat: 40.7128,
            lon: -74.006,
          },
        ],
      });
      setLoading(false);
      return;
    }

    try {
      const token = getToken();
      const headers: Record<string, string> = {};
      if (token) headers['Authorization'] = `Bearer ${token}`;

      const formData = new FormData();
      formData.append('file', selectedFile);

      const res = await fetch(`${API_BASE}/sessions/${sessionId}/analyze`, {
        method: 'POST',
        headers,
        body: formData,
      });

      if (!res.ok) {
        let msg = `Backend error ${res.status}`;
        try {
          msg = await res.text();
        } catch {
          /* ignore JSON error if not json */
        }
        throw new Error(msg);
      }

      const result = (await res.json()) as FitAnalysisResult;
      // Convert camelCase response to what frontend expects, the models we added returning camelCase JSON automatically
      onAnalysisComplete(result);
    } catch (err: unknown) {
      setError(
        err instanceof Error
          ? err.message
          : 'An error occurred during analysis',
      );
    } finally {
      setLoading(false);
    }
  };

  const triggerSync = useGameStore((s) => s.triggerSync);

  const confirmValidation = async (
    analysisResult: FitAnalysisResult | null,
  ): Promise<boolean> => {
    if (!analysisResult || analysisResult.newlyCheckedNodes.length === 0)
      return false;

    setLoading(true);
    try {
      const token = getToken();
      const headers: Record<string, string> = {
        'Content-Type': 'application/json',
      };
      if (token) headers['Authorization'] = `Bearer ${token}`;

      const nodeIds = analysisResult.newlyCheckedNodes.map((n) => n.id);

      const res = await fetch(`${API_BASE}/sessions/${sessionId}/nodes/check`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ nodeIds }),
      });

      if (!res.ok) {
        let msg = `Backend error ${res.status}`;
        try {
          msg = await res.text();
        } catch {
          /* ignore */
        }
        throw new Error(msg);
      }

      setSuccess(
        `Checks sent for ${nodeIds.length} location(s)! Refreshing nodes...`,
      );
      onAnalysisComplete(null);
      triggerSync();
      return true;
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to send checks');
      return false;
    } finally {
      setLoading(false);
    }
  };

  return {
    analyzeFile,
    confirmValidation,
    loading,
    error,
    setError,
    success,
    setSuccess,
  };
}
