import React, { useEffect, useRef } from 'react';
import { MapContainer, TileLayer, Marker, ZoomControl, useMap, useMapEvents, Polyline, Circle } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { Navigation } from 'lucide-react';
import { useGameStore } from '../../store/gameStore';
import { downloadGPXFromPolyline } from '../../lib/geoUtils';
import type { GameSession, MapNode } from '../../types/game';

// Map resizer to handle container boundary updates when Layout triggers changes
const MapResizer = () => {
  const map = useMap();
  useEffect(() => {
    setTimeout(() => map.invalidateSize(), 100);
  }, [map]);
  return null;
};

// Auto-fits map to the bounding box of all nodes whenever they change
const MapAutoFitter = ({ nodes }: { nodes: MapNode[] }) => {
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

interface MapCanvasProps {
  session: GameSession;
  nodes: MapNode[];
}

const MapCanvas = ({ session, nodes }: MapCanvasProps) => {
  const { activePanel, routeData, waypoints, analysisResult, userLocation } = useGameStore();
  const mapRef = useRef<L.Map | null>(null);

  const locateUser = () => {
    if (userLocation && mapRef.current) {
      mapRef.current.setView(userLocation, 16);
    }
  };

  const center: [number, number] = [
    session.center_lat ?? 40.7128, 
    session.center_lon ?? -74.006
  ];

  const parsedPolyline = routeData.polyline ? JSON.parse(routeData.polyline).map((p: [number, number]) => [p[1], p[0]] as [number, number]) : [];
  const analysisPath = analysisResult?.path ? analysisResult.path.map((p: { lat: number, lon: number }) => [p.lat, p.lon] as [number, number]) : [];

  return (
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
          >
            <Navigation className="w-5 h-5 fill-white" />
          </button>
        )}

        <div className="bg-[var(--color-surface-hex)]/90 backdrop-blur-md rounded-xl border border-[var(--color-border-strong-hex)] shadow-2xl overflow-hidden flex flex-col">
           <button
              onClick={() => mapRef.current?.zoomIn()}
              className="p-3 hover:bg-[rgb(var(--color-surface-overlay))] text-[var(--color-text-hex)] transition-colors border-b border-[var(--color-border-hex)]"
           >
             <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
           </button>
           <button
              onClick={() => mapRef.current?.zoomOut()}
              className="p-3 hover:bg-[rgb(var(--color-surface-overlay))] text-[var(--color-text-hex)] transition-colors"
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
  );
};

export default MapCanvas;
