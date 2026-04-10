import React, { useEffect, useState, useCallback, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { MapContainer, TileLayer, Marker, ZoomControl, useMap, useMapEvents, Polyline, Circle } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { Loader2, X } from 'lucide-react';
import { useGameStore } from '../store/gameStore';
import RoutePanel from '../components/game/RoutePanel';
import UploadPanel from '../components/game/UploadPanel';
import ChatPanel from '../components/game/ChatPanel';
import InventoryPanel from '../components/game/InventoryPanel';
import { useArchipelagoStore } from '../store/archipelagoStore';
import { archipelago } from '../lib/archipelago';
import { getToken } from '../store/authStore';
import { useGeolocation } from '../hooks/useGeolocation';
import { Navigation } from 'lucide-react';
import { downloadGPXFromPolyline } from '../lib/geoUtils';

// Map resizer to handle container boundary updates when Layout triggers changes
const MapResizer = () => {
  const map = useMap();
  useEffect(() => {
    setTimeout(() => map.invalidateSize(), 100);
  }, [map]);
  return null;
};

// Auto-fits map to the bounding box of all nodes whenever they change
const MapAutoFitter = ({ nodes }: { nodes: any[] }) => {
  const map = useMap();
  const fittedRef = useRef(false);

  useEffect(() => {
    if (nodes.length === 0 || fittedRef.current) return;
    const latlngs = nodes.map(n => L.latLng(n.lat, n.lon));
    const bounds = L.latLngBounds(latlngs);
    if (bounds.isValid()) {
      map.fitBounds(bounds, { padding: [48, 48], maxZoom: 15 });
      fittedRef.current = true;
    }
  }, [map, nodes]);

  return null;
};

// Map click listener for routing
const MapEvents = () => {
  const { activePanel, addWaypoint } = useGameStore();
  useMapEvents({
    click(e) {
      if (activePanel === 'route') {
        addWaypoint([e.latlng.lat, e.latlng.lng]);
      }
    },
  });
  return null;
};

const NODE_COLORS: Record<string, string> = {
  Hidden: '#525252',     // neutral-600
  Available: '#f97316',  // orange-500
  Checked: '#22c55e'     // green-500
};

const getMarkerIcon = (state: string) => {
  const color = NODE_COLORS[state] || NODE_COLORS.Hidden;
  return L.divIcon({ 
    className: 'custom-div-icon', 
    html: `<div style="background-color:${color}; width:20px; height:20px; border-radius:50%; border:3px solid rgba(255, 255, 255, 0.5); box-shadow:0 0 10px ${color}"></div>`,
    iconSize: [20, 20],
    iconAnchor: [10, 10]
  });
};

const GameStatsBar = ({ session, nodes }: { session: any, nodes: any[] }) => {
  const { status, error } = useArchipelagoStore();
  const [showStatsInfo, setShowStatsInfo] = useState(false);
  const checked = nodes.filter(n => n.state === 'Checked').length;
  const available = nodes.filter(n => n.state === 'Available').length;
  const hidden = nodes.filter(n => n.state === 'Hidden').length;

  const statusColor = status === 'connected' ? 'bg-[var(--color-success-hex)] shadow-[0_0_8px_rgba(var(--color-success),0.5)]' :
                      status === 'connecting' ? 'bg-[var(--color-warning-hex)] animate-pulse' :
                      status === 'error' ? 'bg-[var(--color-error-hex)]' : 'bg-[var(--color-border-hex)]';

  return (
    <div className="absolute top-0 left-0 right-0 bg-[var(--color-surface-hex)]/90 backdrop-blur-md border-b border-[var(--color-border-strong-hex)] px-4 py-2 flex items-center justify-between h-12 z-[1000]">
      <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-[var(--color-border-hex)] bg-[rgb(var(--color-surface-overlay))] relative group min-w-0">
        <div className={`w-2 h-2 rounded-full flex-shrink-0 ${statusColor}`}></div>
        <span className="text-xs font-medium text-[var(--color-text-muted-hex)] truncate">
          {session?.name || session?.ap_seed_name || 'Unnamed Session'} • {session?.ap_slot_name || 'Local Rider'}
        </span>
        {error && (
          <div className="absolute top-full left-0 mt-2 p-2 bg-[var(--color-error-hex)]/90 border border-[var(--color-error-hex)] rounded text-[10px] text-[var(--color-text-hex)] opacity-0 group-hover:opacity-100 transition-opacity z-50 w-48">
            {error}
          </div>
        )}
      </div>

      <div className="relative">
        <button 
          onClick={() => setShowStatsInfo(!showStatsInfo)}
          className="flex items-center gap-4 font-black text-xs uppercase tracking-tight ml-4 p-1.5 rounded-lg hover:bg-[rgb(var(--color-surface-overlay))] transition-colors focus:outline-none focus:ring-2 focus:ring-[var(--color-border-hex)]"
          aria-label="Toggle node statistics"
          aria-expanded={showStatsInfo}
        >
          <div className="flex items-baseline gap-1" title="Hidden Locations">
            <span className="text-[var(--color-text-hex)] leading-none text-sm">{hidden}</span>
          </div>
          <div className="flex items-baseline gap-1" title="Available Locations">
            <span className="text-[var(--color-primary-hex)] leading-none text-sm">{available}</span>
          </div>
          <div className="flex items-baseline gap-1" title="Checked Locations">
            <span className="text-[var(--color-success-hex)] leading-none text-sm">{checked}</span>
          </div>
        </button>

        {showStatsInfo && (
          <div className="absolute top-full right-0 mt-3 w-48 bg-[var(--color-surface-hex)] border border-[var(--color-border-strong-hex)] rounded-xl shadow-xl overflow-hidden z-[1010]">
            <div className="flex flex-col">
              <div className="flex items-center justify-between px-4 py-3 border-b border-[var(--color-border-hex)]/50">
                <div className="flex items-center gap-2">
                  <div className="w-2.5 h-2.5 rounded-full bg-[var(--color-text-hex)]"></div>
                  <span className="text-[10px] font-bold text-[var(--color-text-muted-hex)] tracking-wider uppercase">Hidden</span>
                </div>
                <span className="text-[var(--color-text-hex)] font-bold">{hidden}</span>
              </div>
              <div className="flex items-center justify-between px-4 py-3 border-b border-[var(--color-border-hex)]/50">
                <div className="flex items-center gap-2">
                  <div className="w-2.5 h-2.5 rounded-full bg-[var(--color-primary-hex)]"></div>
                  <span className="text-[10px] font-bold text-[var(--color-text-muted-hex)] tracking-wider uppercase">Available</span>
                </div>
                <span className="text-[var(--color-primary-hex)] font-bold">{available}</span>
              </div>
              <div className="flex items-center justify-between px-4 py-3">
                <div className="flex items-center gap-2">
                  <div className="w-2.5 h-2.5 rounded-full bg-[var(--color-success-hex)]"></div>
                  <span className="text-[10px] font-bold text-[var(--color-text-muted-hex)] tracking-wider uppercase">Checked</span>
                </div>
                <span className="text-[var(--color-success-hex)] font-bold">{checked}</span>
              </div>
            </div>
            {/* Click away layer to close */}
            <div 
               className="fixed inset-0 z-[-1]" 
               onClick={() => setShowStatsInfo(false)}
               aria-hidden="true"
            />
          </div>
        )}
      </div>
    </div>
  );
};

const ArchipelagoReconnectDialog = ({
  error,
  initialUrl,
  initialSlot,
  onRetry,
  onCancel,
}: {
  error: string;
  initialUrl: string;
  initialSlot: string;
  onRetry: (url: string, slot: string, password: string) => void;
  onCancel: () => void;
}) => {
  const [url, setUrl] = React.useState(initialUrl);
  const [slot, setSlot] = React.useState(initialSlot);
  const [password, setPassword] = React.useState('');
  const { status: apStatus } = useArchipelagoStore();
  const isRetrying = apStatus === 'connecting';

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (url.trim() && slot.trim()) onRetry(url.trim(), slot.trim(), password);
  };

  const inputClass = "w-full px-3 py-2 rounded-lg bg-[rgb(var(--color-surface-overlay))] border border-[var(--color-border-hex)] text-xs text-[var(--color-text-hex)] placeholder:text-[var(--color-text-muted-hex)] focus:outline-none focus:border-[var(--color-primary-hex)] transition-colors";

  return (
    <div className="fixed inset-0 z-[2000] flex items-center justify-center p-4" role="dialog" aria-modal="true" aria-labelledby="ap-reconnect-title">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={!isRetrying ? onCancel : undefined} aria-hidden="true" />
      {/* Panel */}
      <div className="relative w-full max-w-sm bg-[var(--color-surface-hex)] border border-[var(--color-border-strong-hex)] rounded-2xl shadow-2xl overflow-hidden">
        {/* Red accent top bar */}
        <div className="h-1 w-full bg-[var(--color-error-hex)]" />
        <div className="p-6">
          {/* Header */}
          <div className="flex items-start gap-3 mb-5">
            <div className="flex-shrink-0 w-9 h-9 rounded-full bg-[var(--color-error-hex)]/15 flex items-center justify-center">
              <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-[var(--color-error-hex)]">
                <circle cx="12" cy="12" r="10"/>
                <line x1="12" y1="8" x2="12" y2="12"/>
                <line x1="12" y1="16" x2="12.01" y2="16"/>
              </svg>
            </div>
            <div className="flex-1 min-w-0">
              <h2 id="ap-reconnect-title" className="text-sm font-bold text-[var(--color-text-hex)] mb-0.5">Archipelago Connection Failed</h2>
              <p className="text-[10px] text-[var(--color-text-muted-hex)] leading-relaxed break-words">{error}</p>
            </div>
          </div>

          {/* Reconnect form */}
          <form onSubmit={handleSubmit} className="space-y-3">
            <div>
              <label htmlFor="ap-reconnect-url" className="block text-[10px] font-bold text-[var(--color-text-muted-hex)] uppercase tracking-wider mb-1">Server URL</label>
              <input
                id="ap-reconnect-url"
                type="text"
                value={url}
                onChange={e => setUrl(e.target.value)}
                placeholder="archipelago.gg:12345"
                className={inputClass}
                disabled={isRetrying}
                autoFocus
              />
            </div>
            <div>
              <label htmlFor="ap-reconnect-slot" className="block text-[10px] font-bold text-[var(--color-text-muted-hex)] uppercase tracking-wider mb-1">Slot Name</label>
              <input
                id="ap-reconnect-slot"
                type="text"
                value={slot}
                onChange={e => setSlot(e.target.value)}
                placeholder="YourSlotName"
                className={inputClass}
                disabled={isRetrying}
              />
            </div>
            <div>
              <label htmlFor="ap-reconnect-password" className="block text-[10px] font-bold text-[var(--color-text-muted-hex)] uppercase tracking-wider mb-1">Password <span className="normal-case font-normal">(optional)</span></label>
              <input
                id="ap-reconnect-password"
                type="password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                placeholder="Leave blank if none"
                className={inputClass}
                disabled={isRetrying}
              />
            </div>

            <div className="flex gap-2 pt-1">
              <button
                type="button"
                onClick={onCancel}
                disabled={isRetrying}
                className="flex-1 px-4 py-2 text-xs font-bold rounded-lg border border-[var(--color-border-hex)] text-[var(--color-text-muted-hex)] hover:bg-[rgb(var(--color-surface-overlay))] transition-colors disabled:opacity-40"
              >
                Cancel
              </button>
              <button
                id="ap-reconnect-submit"
                type="submit"
                disabled={isRetrying || !url.trim() || !slot.trim()}
                className="flex-1 px-4 py-2 text-xs font-bold rounded-lg bg-[var(--color-primary-hex)] hover:opacity-90 text-white transition-opacity active:scale-95 disabled:opacity-50 flex items-center justify-center gap-2"
              >
                {isRetrying ? (
                  <><Loader2 className="w-3 h-3 animate-spin" /> Connecting…</>
                ) : 'Try Again'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

const GameView = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { activePanel, setActivePanel, routeData, waypoints, analysisResult, nodes, setNodes, userLocation, syncVersion } = useGameStore();
  
  const [session, setSession] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState('');
  const [showReconnect, setShowReconnect] = useState(false);
  const [pendingConnection, setPendingConnection] = useState<{ url: string; slot: string } | null>(null);

  const { checkedLocationIds, receivedItems, status: apStatus, error: apError, setCheckedLocations, setReceivedItems } = useArchipelagoStore();

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
  
  const mapRef = useRef<L.Map | null>(null);

  const locateUser = () => {
    if (userLocation && mapRef.current) {
      mapRef.current.setView(userLocation, 16);
    }
  };

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
           setSession({ id: 'mock_session_123', ap_seed_name: 'Mock Seed', ap_slot_name: 'Mock Slot', center_lat: 40.7128, center_lon: -74.006 });
           setNodes([
             { id: 'mock_node_1', name: 'Mock Node 1', lat: 40.7128, lon: -74.006, state: 'Available', ap_location_id: 1001 },
             { id: 'mock_node_2', name: 'Mock Node 2', lat: 40.7158, lon: -74.009, state: 'Available', ap_location_id: 1002 }
           ]);
           return;
        }
        throw new Error('Session not found');
      }
      const sessionData = await sessionRes.json();
      setSession(sessionData);

      const nodesRes = await fetch(`/api/sessions/${id}/nodes`, { headers, signal });
      if (!nodesRes.ok) throw new Error('Failed to load nodes');
      const nodesData = await nodesRes.json();
      setNodes(nodesData);

      if (sessionData.received_item_ids) {
        setReceivedItems(sessionData.received_item_ids.map((id: number) => ({ id, name: `Item ${id}` })));
      }

      // Only connect if the request hasn't been aborted by unmount
      if (!signal.aborted && sessionData.ap_server_url && sessionData.ap_slot_name) {
        console.log(`Connecting to Archipelago: ${sessionData.ap_server_url} as ${sessionData.ap_slot_name}`);
        archipelago.connect(id!, sessionData.ap_server_url, sessionData.ap_slot_name);
      }
    } catch (err: any) {
      if (err.name === 'AbortError') return; // Ignore aborts
      console.error('fetchData error:', err);
      setErrorMsg(err.message);
    } finally {
      if (!signal.aborted) {
        setLoading(false);
      }
    }
  }, [id, setNodes]);

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
    const rawCheckedIds = Array.isArray(checkedLocationIds) ? checkedLocationIds : Array.from((checkedLocationIds as any) || []);
    const checkedIdsSet = new Set(rawCheckedIds);
    
    if (nodes.length > 0 && checkedIdsSet.size > 0) {
      const updatedNodes = nodes.map(node => {
        // Checking both ap_location_id (from DB) and apLocationId (from analyzer) for robustness
        const locId = node.ap_location_id || node.apLocationId;
        if (locId && checkedIdsSet.has(locId)) {
          if (node.state !== 'Checked') {
            return { ...node, state: 'Checked' };
          }
        }
        return node;
      });

      // Avoid infinite loop by only updating if something actually changed
      const hasChanges = updatedNodes.some((node, i) => node.state !== nodes[i].state);
      if (hasChanges) {
        const newlyChecked = updatedNodes.filter((n, i) => n.state === 'Checked' && nodes[i].state !== 'Checked').length;
        console.log(`[ArchipelagoSync] ${newlyChecked} new locations confirmed. Total checked: ${updatedNodes.filter(n => n.state === 'Checked').length}`);
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

  const center: [number, number] = [
    session.center_lat ?? 40.7128, 
    session.center_lon ?? -74.006
  ];

  const parsedPolyline = routeData.polyline ? JSON.parse(routeData.polyline).map((p: [number, number]) => [p[1], p[0]] as [number, number]) : [];
  const analysisPath = analysisResult?.path ? analysisResult.path.map((p: { lat: number, lon: number }) => [p.lat, p.lon] as [number, number]) : [];

  return (
    <div className="relative w-full h-full flex flex-col bg-[var(--color-surface-alt-hex)]">
      {showReconnect && apError && session && (
        <ArchipelagoReconnectDialog
          error={apError}
          initialUrl={session.ap_server_url ?? ''}
          initialSlot={session.ap_slot_name ?? ''}
          onRetry={(url, slot, pw) => {
            setPendingConnection({ url, slot });
            archipelago.connect(id!, url, slot, pw);
          }}
          onCancel={() => setShowReconnect(false)}
        />
      )}
      <GameStatsBar session={session} nodes={nodes} />

      <div className="flex-1 w-full relative flex overflow-hidden pb-20 md:pb-0">
        <div className="flex-1 relative">
          <MapContainer
            center={center}
            zoom={14}
            ref={mapRef}
            style={{ height: '100%', width: '100%', zIndex: 0 }}
            zoomControl={false}
          >
            <TileLayer
              url="https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png"
              attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>'
              maxZoom={19}
            />
            <MapResizer />
            <MapEvents />
            <MapAutoFitter nodes={nodes} />
            <ZoomControl position="bottomleft" />

            {/* Radius circle around session center */}
            {session.center_lat && session.center_lon && session.radius && (
              <Circle
                center={[session.center_lat, session.center_lon]}
                radius={session.radius}
                pathOptions={{
                  color: '#f97316',
                  weight: 2,
                  opacity: 0.5,
                  fillColor: '#f97316',
                  fillOpacity: 0.04,
                  dashArray: '8 6',
                }}
              />
            )}
            
            {nodes.map(node => (
              <Marker 
                key={node.id} 
                position={[node.lat, node.lon]} 
                icon={getMarkerIcon(node.state)} 
                title={node.name}
              />
            ))}

            {parsedPolyline.length > 0 && (
              <Polyline positions={parsedPolyline} color="#f97316" weight={5} opacity={0.7} />
            )}

            {analysisPath.length > 0 && (
              <Polyline positions={analysisPath} color="#22c55e" weight={5} opacity={0.7} dashArray="10, 10" />
            )}

            {waypoints.map((wp, i) => (
              <Marker
                key={`wp-${i}`}
                position={wp}
                icon={L.divIcon({
                  className: 'bg-white border-2 border-orange-500 rounded-full flex items-center justify-center text-[10px] font-bold text-orange-500 leading-none w-5 h-5',
                  html: `<div style="width: 100%; height: 100%; display: flex; align-items: center; justify-content: center;">${i + 1}</div>`,
                  iconSize: [20, 20],
                  iconAnchor: [10, 10]
                })}
              />
            ))}

            {userLocation && (
              <Marker 
                position={userLocation} 
                zIndexOffset={1000}
                icon={L.divIcon({
                  className: 'relative',
                  html: `
                    <div class="relative flex items-center justify-center">
                      <div class="absolute w-8 h-8 bg-blue-500/30 rounded-full animate-ping"></div>
                      <div class="w-4 h-4 bg-blue-500 border-2 border-white rounded-full shadow-lg z-10"></div>
                    </div>
                  `,
                  iconSize: [32, 32],
                  iconAnchor: [16, 16]
                })}
              />
            )}
          </MapContainer>

          {/* Map Controls */}
          <div className="absolute bottom-24 md:bottom-7 left-6 z-10 flex flex-col gap-2">
            {userLocation && (
              <button
                onClick={locateUser}
                className="p-3 bg-blue-600 hover:bg-blue-500 text-white rounded-xl shadow-lg shadow-blue-900/30 transition-all active:scale-95"
                title="Locate Me"
                aria-label="Locate me"
              >
                <Navigation className="w-5 h-5 fill-white" />
              </button>
            )}

            <div className="bg-[var(--color-surface-hex)]/90 backdrop-blur-md rounded-xl border border-[var(--color-border-strong-hex)] shadow-2xl overflow-hidden flex flex-col">
               <button
                  onClick={() => mapRef.current?.zoomIn()}
                  className="p-3 hover:bg-[rgb(var(--color-surface-overlay))] text-[var(--color-text-hex)] transition-colors border-b border-[var(--color-border-hex)]"
                  aria-label="Zoom in"
               >
                 <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
               </button>
               <button
                  onClick={() => mapRef.current?.zoomOut()}
                  className="p-3 hover:bg-[rgb(var(--color-surface-overlay))] text-[var(--color-text-hex)] transition-colors"
                  aria-label="Zoom out"
               >
                 <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><line x1="5" y1="12" x2="19" y2="12"></line></svg>
               </button>
            </div>
          </div>

          {/* Quick Stats Overlay (Floating when no panel open) */}
          {!activePanel && (
            <div className="absolute bottom-4 left-4 right-4 md:right-4 md:left-auto md:w-80 bg-[var(--color-surface-hex)]/90 backdrop-blur-md rounded-xl p-4 border border-[var(--color-border-strong-hex)] z-5 flex justify-between items-center shadow-2xl">
                <div className="flex gap-4">
                  <div className="flex flex-col">
                    <span className="text-[10px] font-bold text-[var(--color-text-muted-hex)] tracking-wider uppercase">Distance</span>
                    <span className="text-[var(--color-text-hex)] font-bold text-lg leading-none">{routeData.distance.toFixed(2)}<span className="text-xs text-[var(--color-text-muted-hex)] font-normal ml-1">km</span></span>
                  </div>
                  <div className="flex flex-col">
                    <span className="text-[10px] font-bold text-[var(--color-text-muted-hex)] tracking-wider uppercase">Elev Gain</span>
                    <span className="text-[var(--color-text-hex)] font-bold text-lg leading-none">{routeData.elevation.toFixed(0)}<span className="text-xs text-[var(--color-text-muted-hex)] font-normal ml-1">m</span></span>
                  </div>
                </div>
                {routeData.polyline && (
                  <button onClick={() => downloadGPXFromPolyline(routeData.polyline as string)} className="px-3 py-2 bg-orange-600 hover:bg-orange-500 rounded-lg text-xs font-bold text-white transition-colors flex items-center gap-2">
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
                    GPX
                  </button>
                )}
            </div>
          )}
        </div>

        {/* Side Panels */}
        {activePanel && (
          <div className="w-full md:w-96 border-l border-[var(--color-border-hex)] flex flex-col bg-[var(--color-surface-hex)] z-10 absolute inset-0 md:relative md:bg-[var(--color-surface-alt-hex)]">
             <div className="flex items-center justify-between p-4 border-b border-[var(--color-border-hex)] md:hidden">
                <span className="font-bold text-[var(--color-text-hex)] uppercase tracking-widest text-xs">{activePanel}</span>
                <button onClick={() => setActivePanel(null)} className="p-2 hover:bg-[rgb(var(--color-surface-overlay))] rounded-lg text-[var(--color-text-muted-hex)]" aria-label="Close panel">
                  <X className="w-5 h-5" />
                </button>
             </div>
             
             {activePanel === 'route' && <RoutePanel />}
             {activePanel === 'upload' && <UploadPanel sessionId={id!} />}
             {activePanel === 'chat' && <ChatPanel />}
             {activePanel === 'inventory' && <InventoryPanel />}
          </div>
        )}
      </div>
    </div>
  );
};

export default GameView;
