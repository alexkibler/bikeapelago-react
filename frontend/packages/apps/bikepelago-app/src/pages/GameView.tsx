import { useEffect, useMemo, useState } from 'react';

import { Loader2 } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';

// Extracted Components
import ArchipelagoReconnectDialog from '../components/game/ArchipelagoReconnectDialog';
import DebugBanner from '../components/game/DebugBanner';
import GameStatsBar from '../components/game/GameStatsBar';
import MapCanvas from '../components/game/MapCanvas';
import SidePanelCoordinator from '../components/game/SidePanelCoordinator';
import { useGeolocation } from '../hooks/useGeolocation';
import { useToast } from '../hooks/useToast';
import { archipelago } from '../lib/archipelago';
import { useSessionNodesGet } from '../operations/sessionNodes';
import { useSessionGet, useSessionUpdate } from '../operations/sessions';
import { useArchipelagoStore } from '../store/archipelagoStore';
import { useGameStore } from '../store/gameStore';

const GameView = () => {
  const { id: idParam } = useParams<{ id: string }>();
  const id = idParam!;
  const navigate = useNavigate();
  const toast = useToast();

  const activePanel = useGameStore((s) => s.activePanel);
  const nodes = useGameStore((s) => s.nodes);
  const setNodes = useGameStore((s) => s.setNodes);
  const syncVersion = useGameStore((s) => s.syncVersion);

  const [reconnectCanceled, setReconnectCanceled] = useState<boolean | null>(
    null,
  );
  const [pendingConnection, setPendingConnection] = useState<{
    url: string;
    slot: string;
  } | null>(null);

  const checkedLocationIds = useArchipelagoStore((s) => s.checkedLocationIds);
  const apStatus = useArchipelagoStore((s) => s.status);
  const apError = useArchipelagoStore((s) => s.error);
  const setReceivedItems = useArchipelagoStore((s) => s.setReceivedItems);

  const sessionReq = useSessionGet({ id, syncVersion });
  const sessionUpdate = useSessionUpdate({ id });

  const sessionNodesReq = useSessionNodesGet({ sessionId: id, syncVersion });

  const session = sessionReq.data;
  const setSession = useGameStore((s) => s.setSession);

  const showReconnect = useMemo(() => {
    if (apStatus === 'error' && apError) {
      return true;
    }

    if (reconnectCanceled) {
      return false;
    }

    return false;
  }, [apStatus, apError, reconnectCanceled]);

  useEffect(() => {
    if (session) setSession(session);
  }, [session, setSession]);

  useEffect(() => {
    if (sessionNodesReq.data) {
      setNodes(sessionNodesReq.data);
    }
  }, [sessionNodesReq.data, setNodes]);

  useEffect(() => {
    if (session?.received_item_ids) {
      const ITEM_MAP: Record<number, string> = {
        802001: 'Goal',
        802002: 'North Quadrant Pass',
        802003: 'South Quadrant Pass',
        802004: 'East Quadrant Pass',
        802005: 'West Quadrant Pass',
        802006: 'Progressive Radius Increase',
        802010: 'Detour',
        802011: 'Drone',
        802012: 'Signal Amplifier'
      };

      setReceivedItems(
        session.received_item_ids.map((itemId: number) => ({
          id: itemId,
          name: ITEM_MAP[itemId] || `Item ${itemId}`,
        })),
      );
    }
  }, [session, setReceivedItems]);

  // Save new connection info to DB on successful reconnect
  useEffect(() => {
    if (apStatus !== 'connected' || !pendingConnection || !session) return;
    sessionUpdate.mutate(
      {
        ap_server_url: pendingConnection.url,
        ap_slot_name: pendingConnection.slot,
      },
      {
        onSuccess: () => {
          toast.success('Connection settings saved');
          setPendingConnection(null);
        },
        onError: (err) => {
          toast.error('Failed to save connection settings');
          console.error('Failed to save connection info:', err);
          setPendingConnection(null);
        },
      },
    );
  }, [apStatus, pendingConnection, session, toast, setPendingConnection]);

  // Activate Geolocation tracking
  useGeolocation();

  // Effect for Archipelago connection (depends only on session loading)
  useEffect(() => {
    if (id && session) {
      if (session.ap_server_url && session.ap_slot_name) {
        archipelago.connect(id, session.ap_server_url, session.ap_slot_name);
      } else {
        archipelago.joinSessionOnly(id);
      }
    }
  }, [id, session]);

  // Handle cleanup on unmount or session ID change
  useEffect(() => {
    return () => {
      archipelago.disconnect();
    };
  }, [id]);

  // Sync Archipelago checked locations with local node states
  useEffect(() => {
    if (nodes.length === 0 || checkedLocationIds.length === 0) return;

    const checkedIdsSet = new Set(checkedLocationIds);
    let hasChanges = false;

    const updatedNodes = nodes.map((node) => {
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

  const loading = sessionReq.isLoading || sessionNodesReq.isLoading;

  if (loading) {
    return (
      <div className='h-full flex items-center justify-center bg-[var(--color-surface-alt-hex)]'>
        <Loader2 className='w-8 h-8 animate-spin text-[var(--color-primary-hex)]' />
      </div>
    );
  }

  if (!session) {
    return (
      <div className='h-full flex flex-col items-center justify-center space-y-4 bg-[var(--color-surface-alt-hex)]'>
        <p className='text-[var(--color-error-hex)] font-bold'>
          Session not found
        </p>
        <button
          onClick={() => navigate('/')}
          className='btn-orange px-6 py-2 rounded-lg font-bold'
        >
          Back to Sessions
        </button>
      </div>
    );
  }

  const reconnectProps = {
    error: apError || '',
    initialUrl: session.ap_server_url ?? '',
    initialSlot: session.ap_slot_name ?? '',
    onRetry: (url: string, slot: string, password: string) => {
      setPendingConnection({ url, slot });
      archipelago.connect(id, url, slot, password);
    },
    onCancel: () => setReconnectCanceled(true),
  };

  return (
    <div className='relative w-full h-full flex flex-col bg-[var(--color-surface-alt-hex)]'>
      {showReconnect && <ArchipelagoReconnectDialog {...reconnectProps} />}
      <DebugBanner />
      <GameStatsBar session={session} nodes={nodes} />

      <div className='flex-1 w-full relative flex overflow-hidden pb-20 md:pb-0'>
        <MapCanvas session={session} nodes={nodes} />
        {activePanel && <SidePanelCoordinator panel={activePanel} />}
      </div>
    </div>
  );
};

export default GameView;
