import React, { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Loader2 } from 'lucide-react';
import { useGameStore } from '../store/gameStore';
import { useArchipelagoStore } from '../store/archipelagoStore';
import { archipelago } from '../lib/archipelago';
import { getToken } from '../store/authStore';
import { useGeolocation } from '../hooks/useGeolocation';

// Extracted Components
import ArchipelagoReconnectDialog from '../components/game/ArchipelagoReconnectDialog';
import GameStatsBar from '../components/game/GameStatsBar';
import MapCanvas from '../components/game/MapCanvas';
import SidePanelCoordinator from '../components/game/SidePanelCoordinator';
import type { GameSession, MapNode } from '../types/game';

const GameView = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { activePanel, nodes, setNodes, syncVersion } = useGameStore();
  
  const [session, setSession] = useState<GameSession | null>(null);
  const [loading, setLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState('');
  const [showReconnect, setShowReconnect] = useState(false);
  const [pendingConnection, setPendingConnection] = useState<{ url: string; slot: string } | null>(null);

  const { checkedLocationIds, status: apStatus, error: apError, setReceivedItems } = useArchipelagoStore();

  // Show reconnect dialog on connection failure; hide it if we successfully connect
  useEffect(() => {
    if (apStatus === 'error' && apError) {
      setShowReconnect(true);
    } else if (apStatus === 'connected') {
      setShowReconnect(false);
    }
  }, [apStatus, apError]);

  // Save new connection info to DB on successful reconnect
  useEffect(() => {
    if (apStatus === 'connected' && pendingConnection && session) {
      const updateSessionDB = async () => {
        try {
          const token = getToken();
          const headers = {
            'Content-Type': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {})
          };
          const res = await fetch(`/api/sessions/${session.id}`, {
            method: 'PATCH',
            headers,
            body: JSON.stringify({
              ap_server_url: pendingConnection.url,
              ap_slot_name: pendingConnection.slot
            })
          });
          if (res.ok) {
            const updated = await res.json();
            setSession(updated);
          }
        } catch (err) {
          console.error('Failed to save updated connection info', err);
        } finally {
          setPendingConnection(null);
        }
      };
      updateSessionDB();
    }
  }, [apStatus, pendingConnection, session]);
  
  // Activate Geolocation tracking
  useGeolocation();

  const fetchData = useCallback(async (signal: AbortSignal) => {
    try {
      const token = getToken();
      const headers = {
        'Content-Type': 'application/json',
        ...(token ? { 'Authorization': `Bearer ${token}` } : {})
      };

      const sessionRes = await fetch(`/api/sessions/${id}`, { headers, signal });
      if (!sessionRes.ok) {
        if (id === 'mock_session_123') {
           setSession({ id: 'mock_session_123', ap_seed_name: 'Mock Seed', ap_slot_name: 'Mock Slot', center_lat: 40.7128, center_lon: -74.006 } as GameSession);
           setNodes([
             { id: 'mock_node_1', name: 'Mock Node 1', lat: 40.7128, lon: -74.006, state: 'Available', ap_location_id: 1001 },
             { id: 'mock_node_2', name: 'Mock Node 2', lat: 40.7158, lon: -74.009, state: 'Available', ap_location_id: 1002 }
           ] as MapNode[]);
           return;
        }
        throw new Error('Session not found');
      }
      const sessionData = await sessionRes.json();
      setSession(sessionData as GameSession);

      const nodesRes = await fetch(`/api/sessions/${id}/nodes`, { headers, signal });
      if (!nodesRes.ok) throw new Error('Failed to load nodes');
      const nodesData = await nodesRes.json();
      setNodes(nodesData as MapNode[]);

      if (sessionData.received_item_ids) {
        setReceivedItems(sessionData.received_item_ids.map((id: number) => ({ id, name: `Item ${id}` })));
      }

      // Only connect if the request hasn't been aborted by unmount
      if (!signal.aborted && sessionData.ap_server_url && sessionData.ap_slot_name) {
        console.log(`Connecting to Archipelago: ${sessionData.ap_server_url} as ${sessionData.ap_slot_name}`);
        archipelago.connect(id!, sessionData.ap_server_url, sessionData.ap_slot_name);
      }
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') return; 
      console.error('fetchData error:', err);
      setErrorMsg(err instanceof Error ? err.message : 'An unknown error occurred');
    } finally {
      if (!signal.aborted) {
        setLoading(false);
      }
    }
  }, [id, setNodes, setReceivedItems]);

  useEffect(() => {
    const controller = new AbortController();
    
    if (id) {
      fetchData(controller.signal);
    }
    return () => {
      controller.abort();
      archipelago.disconnect();
    };
  }, [id, fetchData, syncVersion]);

  // Sync Archipelago checked locations with local node states
  useEffect(() => {
    const rawCheckedIds = checkedLocationIds;
    const checkedIdsSet = new Set(rawCheckedIds);
    
    if (nodes.length > 0 && checkedIdsSet.size > 0) {
      const updatedNodes: MapNode[] = nodes.map(node => {
        const locId = node.ap_location_id || node.apLocationId;
        if (locId && checkedIdsSet.has(locId)) {
          if (node.state !== 'Checked') {
            return { ...node, state: 'Checked' as const };
          }
        }
        return node;
      });

      const hasChanges = updatedNodes.some((node, i) => node.state !== nodes[i].state);
      if (hasChanges) {
        setNodes(updatedNodes);
      }
    }
  }, [checkedLocationIds, nodes, setNodes]);

  if (loading) {
    return (
      <div className="h-full flex items-center justify-center bg-[var(--color-surface-alt-hex)]">
        <Loader2 className="w-8 h-8 animate-spin text-[var(--color-primary-hex)]" />
      </div>
    );
  }

  if (errorMsg || !session) {
    return (
      <div className="h-full flex flex-col items-center justify-center space-y-4 bg-[var(--color-surface-alt-hex)]">
        <p className="text-[var(--color-error-hex)] font-bold">{errorMsg || 'Session not found'}</p>
        <button onClick={() => navigate('/')} className="btn-orange px-6 py-2 rounded-lg font-bold">Back to Sessions</button>
      </div>
    );
  }

  const reconnectProps = {
    error: apError || '',
    initialUrl: session.ap_server_url ?? '',
    initialSlot: session.ap_slot_name ?? '',
    onRetry: (url: string, slot: string, password: string) => {
      setPendingConnection({ url, slot });
      archipelago.connect(id!, url, slot, password);
    },
    onCancel: () => setShowReconnect(false)
  };

  return (
    <div className="relative w-full h-full flex flex-col bg-[var(--color-surface-alt-hex)]">
      {showReconnect && <ArchipelagoReconnectDialog {...reconnectProps} />}
      <GameStatsBar session={session} nodes={nodes} />

      <div className="flex-1 w-full relative flex overflow-hidden pb-20 md:pb-0">
        <MapCanvas session={session} nodes={nodes} />
        {activePanel && <SidePanelCoordinator panel={activePanel} />}
      </div>
    </div>
  );
};

export default GameView;
