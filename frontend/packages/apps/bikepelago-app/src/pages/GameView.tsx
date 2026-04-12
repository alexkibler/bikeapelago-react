import React, { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Loader2 } from 'lucide-react';
import { useGameStore } from '../store/gameStore';
import { useArchipelagoStore } from '../store/archipelagoStore';
import { archipelago } from '../lib/archipelago';
import { useGeolocation } from '../hooks/useGeolocation';
import { apiClient } from '../lib/apiClient';
import { useToast } from '../hooks/useToast';

// Extracted Components
import ArchipelagoReconnectDialog from '../components/game/ArchipelagoReconnectDialog';
import GameStatsBar from '../components/game/GameStatsBar';
import MapCanvas from '../components/game/MapCanvas';
import SidePanelCoordinator from '../components/game/SidePanelCoordinator';
import type { GameSession, MapNode } from '../types/game';

const GameView = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const toast = useToast();
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
    if (apStatus !== 'connected' || !pendingConnection || !session) return;

    const updateSessionDB = async () => {
      try {
        const updated = await apiClient.patch<GameSession>(
          `/api/sessions/${session.id}`,
          {
            ap_server_url: pendingConnection.url,
            ap_slot_name: pendingConnection.slot
          }
        );
        setSession(updated);
        toast.success('Connection settings saved');
      } catch (err) {
        toast.error('Failed to save connection settings');
        console.error('Failed to save connection info:', err);
      } finally {
        setPendingConnection(null);
      }
    };

    updateSessionDB();
  }, [apStatus, pendingConnection, session, toast]);
  
  // Activate Geolocation tracking
  useGeolocation();

  const fetchData = useCallback(async (signal: AbortSignal) => {
    if (!id) return;

    try {
      const sessionData = await apiClient.get<GameSession>(`/api/sessions/${id}`, signal);
      setSession(sessionData);

      const nodesData = await apiClient.get<MapNode[]>(`/api/sessions/${id}/nodes`, signal);
      setNodes(nodesData);

      if (sessionData.received_item_ids) {
        setReceivedItems(sessionData.received_item_ids.map((itemId: number) => ({ id: itemId, name: `Item ${itemId}` })));
      }

      // Only connect if the request hasn't been aborted by unmount
      // Note: Password is not stored; users must re-enter it on reconnect for security
      if (!signal.aborted && sessionData.ap_server_url && sessionData.ap_slot_name) {
        archipelago.connect(id, sessionData.ap_server_url, sessionData.ap_slot_name);
      }
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') return;
      const message = err instanceof Error ? err.message : 'An unknown error occurred';
      setErrorMsg(message);
      toast.error(message);
    } finally {
      if (!signal.aborted) {
        setLoading(false);
      }
    }
  }, [id, setNodes, setReceivedItems, toast]);

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
    if (nodes.length === 0 || checkedLocationIds.length === 0) return;

    const checkedIdsSet = new Set(checkedLocationIds);
    let hasChanges = false;

    const updatedNodes = nodes.map(node => {
      if (checkedIdsSet.has(node.apLocationId) && node.state !== 'Checked') {
        hasChanges = true;
        return { ...node, state: 'Checked' as const };
      }
      return node;
    });

    if (hasChanges) {
      setNodes(updatedNodes);
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
