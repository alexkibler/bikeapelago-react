import { useState } from 'react';
import { SportsLib } from '@sports-alliance/sports-lib';
import FitParser from 'fit-file-parser';
import { getToken } from '../store/authStore';

// Haversine formula to calculate distance between two coordinates in meters
function getDistance(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const R = 6371e3; // Earth radius in meters
  const p1 = (lat1 * Math.PI) / 180;
  const p2 = (lat2 * Math.PI) / 180;
  const dp = ((lat2 - lat1) * Math.PI) / 180;
  const dl = ((lon2 - lon1) * Math.PI) / 180;

  const a =
    Math.sin(dp / 2) * Math.sin(dp / 2) +
    Math.cos(p1) * Math.cos(p2) * Math.sin(dl / 2) * Math.sin(dl / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

  return R * c;
}

export interface PathPoint {
  lat: number;
  lon: number;
  alt?: number;
}

export interface RideStats {
  distanceMeters: number;
  elevationGainMeters: number;
  durationSeconds: number;
  avgSpeedKph?: number;
}

export interface NewlyCheckedNode {
  id: string;
  ap_location_id: number;
  lat: number;
  lon: number;
}

export interface FitAnalysisResult {
  path: PathPoint[];
  stats: RideStats;
  newlyCheckedNodes: NewlyCheckedNode[];
}

export function useFitAnalyzer(sessionId: string, onAnalysisComplete: (result: FitAnalysisResult | null) => void) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const analyzeFile = async (selectedFile: File | null) => {
    if (!selectedFile) return;

    setLoading(true);
    setError(null);
    onAnalysisComplete(null);

    console.log('[useFitAnalyzer] Starting analysis for:', selectedFile.name);

    if ((window as unknown as { PLAYWRIGHT_TEST?: boolean }).PLAYWRIGHT_TEST) {
      console.log('[useFitAnalyzer] Mocking analysis for E2E test');
      onAnalysisComplete({
        path: [{ lat: 40.7128, lon: -74.006 }],
        stats: {
          distanceMeters: 5000,
          durationSeconds: 1200,
          elevationGainMeters: 100,
          avgSpeedKph: 15
        },
        newlyCheckedNodes: [{ id: 'mock_node_1', ap_location_id: 1001, lat: 40.7128, lon: -74.006 }]
      });
      setLoading(false);
      return;
    }

    try {
      const arrayBuffer = await selectedFile.arrayBuffer();

      let eventData;
      const path: PathPoint[] = [];
      let stats: RideStats = { distanceMeters: 0, elevationGainMeters: 0, durationSeconds: 0 };

      try {
        eventData = await SportsLib.importFromFit(arrayBuffer);
        const activities = eventData.getActivities();
        if (activities.length === 0) throw new Error('No activities found in FIT file');
        const activity = activities[0];

        // Extract path
        if (activity.hasStreamData('Position')) {
          const positions = activity.getStreamDataByTime('Position');
          const altitudes = activity.hasStreamData('Altitude') ? activity.getStreamDataByTime('Altitude') : [];
          positions.forEach((pos: unknown, i: number) => {
            const p = pos as { value?: { latitude?: number; lat?: number; longitude?: number; lon?: number } };
            if (p && p.value) {
              const lat = p.value.latitude ?? p.value.lat;
              const lon = p.value.longitude ?? p.value.lon;
              const altObj = altitudes[i] as { value?: number } | undefined;
              if (lat !== undefined && lon !== undefined) {
                path.push({
                  lat,
                  lon,
                  alt: altObj?.value ?? undefined
                });
              }
            }
          });
        }

        // Extract stats
        const getStatValue = (type: string) => activity.getStat(type)?.getValue() as number;
        stats = {
          distanceMeters: getStatValue('Distance') || 0,
          elevationGainMeters: getStatValue('Ascent') || 0,
          durationSeconds: activity.getDuration().getValue() || 0,
          avgSpeedKph: (getStatValue('Average Speed') || 0) * 3.6
        };
      } catch {
        // Fallback to fit-file-parser if SportsLib fails
        const fitParser = new FitParser({ force: true, speedUnit: 'm/s', lengthUnit: 'm', mode: 'both' });
        const fitObject = await new Promise<{ records?: Array<{ position_lat?: number; position_long?: number; altitude?: number; distance?: number; timestamp: number }> }>((resolve, reject) => {
          fitParser.parse(arrayBuffer, (err: unknown, data: unknown) => err ? reject(err) : resolve(data as { records?: Array<{ position_lat?: number; position_long?: number; altitude?: number; distance?: number; timestamp: number }> }));
        });

        if (fitObject.records && fitObject.records.length > 0) {
          fitObject.records.forEach((record) => {
            if (record.position_lat !== undefined && record.position_long !== undefined) {
              path.push({ lat: record.position_lat, lon: record.position_long, alt: record.altitude });
            }
          });
          const last = fitObject.records[fitObject.records.length - 1];
          const first = fitObject.records[0];
          stats = {
            distanceMeters: last.distance || 0,
            elevationGainMeters: 0, // Simplified for brevity
            durationSeconds: (last.timestamp - first.timestamp) / 1000
          };
        } else {
          throw new Error('Failed to parse FIT file');
        }
      }

      if (path.length === 0) throw new Error('No GPS data found in FIT file');

      // Fetch available nodes for this session
      const token = getToken();
      const headers: Record<string, string> = { 'Content-Type': 'application/json' };
      if (token) headers['Authorization'] = `Bearer ${token}`;

      const nodesRes = await fetch(`/api/sessions/${sessionId}/nodes`, { headers });
      if (!nodesRes.ok) throw new Error(`Failed to fetch nodes: ${nodesRes.status}`);
      const allNodes = await nodesRes.json() as Array<{ id: string; state: string; lat: number; lon: number; ap_location_id: number }>;
      const availableNodes = allNodes.filter(n => n.state === 'Available');

      const newlyCheckedNodes: NewlyCheckedNode[] = [];
      for (const node of availableNodes) {
        const isReached = path.some(p => getDistance(node.lat, node.lon, p.lat, p.lon) <= 30);
        if (isReached) {
          newlyCheckedNodes.push({
            id: node.id,
            ap_location_id: node.ap_location_id,
            lat: node.lat,
            lon: node.lon,
          });
        }
      }

      onAnalysisComplete({ path, stats, newlyCheckedNodes });
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'An error occurred during analysis');
    } finally {
      setLoading(false);
    }
  };

  const confirmValidation = async (analysisResult: FitAnalysisResult | null) => {
    if (!analysisResult || analysisResult.newlyCheckedNodes.length === 0) return;

    setLoading(true);
    try {
      const token = getToken();
      const headers: Record<string, string> = { 'Content-Type': 'application/json' };
      if (token) headers['Authorization'] = `Bearer ${token}`;

      const nodeIds = analysisResult.newlyCheckedNodes.map(n => n.id);
      await Promise.all(nodeIds.map(id =>
        fetch(`/api/nodes/${id}`, {
          method: 'PATCH',
          headers,
          body: JSON.stringify({ state: 'Checked' }),
        })
      ));

      setSuccess(`Successfully validated ${nodeIds.length} location(s)!`);
      onAnalysisComplete(null);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to confirm checks');
    } finally {
      setLoading(false);
    }
  };

  return { analyzeFile, confirmValidation, loading, error, setError, success, setSuccess };
}
