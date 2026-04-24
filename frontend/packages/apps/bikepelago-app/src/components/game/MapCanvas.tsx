import { useMemo, useRef, useState } from 'react';
import { MapContainer, TileLayer, Marker, ZoomControl, Polyline, Circle, Polygon } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { Navigation } from 'lucide-react';
import { useGameStore } from '../../store/gameStore';
import { downloadGPX } from '../../lib/geoUtils';
import Stat from '../layout/Stat';
import type { GameSession, MapNode, NodeState } from '../../types/game';
import { MapResizer, MapAutoFitter, MapEvents, MapClickConfirmDialog } from './MapControls';

// Z-index constants
const Z_INDEX = {
  MAP_OVERLAY: 0,
  USER_LOCATION: 1000,
  CONTROLS: 1001
} as const;

const NODE_COLORS: Record<NodeState, string> = {
  Hidden: '#525252',     // neutral-600
  Available: '#f97316',  // orange-500
  Checked: '#22c55e'     // green-500
};

const getMarkerIcon = (state: NodeState, debugClickable = false, selected = false) => {
  const color = NODE_COLORS[state] || NODE_COLORS.Hidden;
  const cursor = debugClickable ? 'cursor:pointer;' : '';
  const ring = selected
    ? `box-shadow:0 0 0 4px rgba(139,92,246,0.8), 0 0 12px ${color};`  // purple selection ring
    : debugClickable
      ? `box-shadow:0 0 0 3px rgba(234,179,8,0.6), 0 0 10px ${color};`
      : `box-shadow:0 0 10px ${color};`;
  
  // Use a 44px hit area for mobile friendliness
  return L.divIcon({
    className: 'custom-div-icon',
    html: `
      <div style="display:flex; align-items:center; justify-content:center; width:44px; height:44px;">
        <div style="background-color:${color}; width:20px; height:20px; border-radius:50%; border:3px solid rgba(255, 255, 255, 0.5); ${ring} ${cursor}"></div>
      </div>
    `,
    iconSize: [44, 44],
    iconAnchor: [22, 22]
  });
};

const getCustomOriginIcon = () =>
  L.divIcon({
    className: 'custom-div-icon',
    html: `
      <div style="display:flex; align-items:center; justify-content:center; width:44px; height:44px;">
        <div style="
          width:26px; height:26px; border-radius:50%;
          background:linear-gradient(135deg,#7c3aed,#4f46e5);
          border:3px solid #fff;
          box-shadow:0 0 0 2px #7c3aed, 0 4px 12px rgba(124,58,237,0.5);
          display:flex; align-items:center; justify-content:center;
          font-size:11px; font-weight:900; color:#fff; cursor:pointer;
        ">S</div>
      </div>
    `,
    iconSize: [44, 44],
    iconAnchor: [22, 22],
  });

interface MapCanvasProps {
  session: GameSession;
  nodes: MapNode[];
}

const ProgressionOverlay = ({ session }: { session: GameSession }) => {
  if (!session.center_lat || !session.center_lon || !session.radius) return null;

  const center: [number, number] = [session.center_lat, session.center_lon];
  const hubRadius = session.radius * 0.25;
  const outerRadius = session.radius;

  const getWedgePoints = (startDeg: number, endDeg: number, r: number, innerR: number = 0): [number, number][] => {
    const points: [number, number][] = [];
    const step = 5; // degrees
    
    // Normalize angles
    let start = startDeg;
    let end = endDeg;
    if (end <= start) end += 360;

    // Outer arc
    for (let a = start; a <= end; a += step) {
      const rad = (90 - a) * (Math.PI / 180);
      const lat = center[0] + (r / 111132) * Math.sin(rad);
      const lon = center[1] + (r / (111132 * Math.cos(center[0] * Math.PI / 180))) * Math.cos(rad);
      points.push([lat, lon]);
    }
    const finalRad = (90 - end) * (Math.PI / 180);
    points.push([
      center[0] + (r / 111132) * Math.sin(finalRad),
      center[1] + (r / (111132 * Math.cos(center[0] * Math.PI / 180))) * Math.cos(finalRad)
    ]);

    if (innerR > 0) {
      // Inner arc (backwards)
      for (let a = end; a >= start; a -= step) {
        const rad = (90 - a) * (Math.PI / 180);
        const lat = center[0] + (innerR / 111132) * Math.sin(rad);
        const lon = center[1] + (innerR / (111132 * Math.cos(center[0] * Math.PI / 180))) * Math.cos(rad);
        points.push([lat, lon]);
      }
      const startRad = (90 - start) * (Math.PI / 180);
      points.push([
        center[0] + (innerR / 111132) * Math.sin(startRad),
        center[1] + (innerR / (111132 * Math.cos(center[0] * Math.PI / 180))) * Math.cos(startRad)
      ]);
    } else {
      points.unshift(center);
    }
    
    return points;
  };

  const getArcPoints = (startDeg: number, endDeg: number, r: number): [number, number][] => {
    const points: [number, number][] = [];
    const step = 5;
    let start = startDeg;
    let end = endDeg;
    if (end <= start) end += 360;
    for (let a = start; a <= end; a += step) {
      const rad = (90 - a) * (Math.PI / 180);
      const lat = center[0] + (r / 111132) * Math.sin(rad);
      const lon = center[1] + (r / (111132 * Math.cos(center[0] * Math.PI / 180))) * Math.cos(rad);
      points.push([lat, lon]);
    }
    return points;
  };

  const unlockedColor = '#f97316';
  const lockedColor = '#525252';

  // For Radius mode, show all 4 concentric rings
  if (session.progression_mode === 'radius') {
    const steps = [
      { r: 0.25, unlocked: true }, // Hub always unlocked
      { r: 0.50, unlocked: session.radius_step >= 1 },
      { r: 0.75, unlocked: session.radius_step >= 2 },
      { r: 1.00, unlocked: session.radius_step >= 3 },
    ];
    
    return (
      <>
        {steps.map((step, i) => (
          <Circle 
            key={i}
            center={center} 
            radius={step.r * session.radius!} 
            pathOptions={{ 
              color: step.unlocked ? unlockedColor : lockedColor, 
              weight: step.unlocked ? 2 : 1, 
              fillColor: step.unlocked ? unlockedColor : lockedColor, 
              fillOpacity: step.unlocked ? 0.04 : 0,
              dashArray: step.unlocked ? undefined : '5,5'
            }} 
          />
        ))}
      </>
    );
  }

  // For Quadrant mode, draw the 4 wedges
  const quadrants = [
    { name: 'North', start: 315, end: 45, unlocked: session.north_pass_received },
    { name: 'East', start: 45, end: 135, unlocked: session.east_pass_received },
    { name: 'South', start: 135, end: 225, unlocked: session.south_pass_received },
    { name: 'West', start: 225, end: 315, unlocked: session.west_pass_received },
  ];

  return (
    <>
      {/* The Hub is always unlocked, but we only draw its stroke for locked quadrants below */}
      <Circle center={center} radius={hubRadius} pathOptions={{ color: unlockedColor, weight: 0, fillColor: unlockedColor, fillOpacity: 0.1 }} />
      
      {quadrants.map(q => (
        <div key={q.name}>
          <Polygon
            positions={getWedgePoints(q.start, q.end, outerRadius, q.unlocked ? 0 : hubRadius)}
            pathOptions={{
              color: q.unlocked ? unlockedColor : lockedColor,
              weight: q.unlocked ? 2 : 1,
              fillColor: q.unlocked ? unlockedColor : lockedColor,
              fillOpacity: q.unlocked ? 0.1 : 0.03, // Match hub opacity when unlocked
              dashArray: q.unlocked ? undefined : '5,5'
            }}
          />
          {!q.unlocked && (
            <Polyline 
              positions={getArcPoints(q.start, q.end, hubRadius)} 
              pathOptions={{ color: unlockedColor, weight: 2 }} 
            />
          )}
        </div>
      ))}
    </>
  );
};

const MapCanvas = ({ session, nodes }: MapCanvasProps) => {
  const activePanel = useGameStore(s => s.activePanel);
  const routeData = useGameStore(s => s.routeData);
  const waypoints = useGameStore(s => s.waypoints);
  const analysisResult = useGameStore(s => s.analysisResult);
  const userLocation = useGameStore(s => s.userLocation);
  const customOrigin = useGameStore(s => s.customOrigin);
  const setCustomOrigin = useGameStore(s => s.setCustomOrigin);
  const selectedNodeIds = useGameStore(s => s.selectedNodeIds);
  const toggleSelectedNode = useGameStore(s => s.toggleSelectedNode);
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

  // Analysis path mapping is relatively simple but good to keep memoized
  const analysisPath = useMemo(() => {
    return analysisResult?.path ? analysisResult.path.map((p: { lat: number, lon: number }) => [p.lat, p.lon] as [number, number]) : [];
  }, [analysisResult]);

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
        <MapEvents nodes={nodes} />
        <MapAutoFitter nodes={nodes} />
        <ZoomControl position="bottomleft" />

        <ProgressionOverlay session={session} />
        
        {nodes.map(node => {
          const isSelected = selectedNodeIds.has(node.id);
          const inRouteMode = activePanel === 'route';
          const inInventoryMode = activePanel === 'inventory';
          
          // Available nodes are selectable when either Route or Inventory panel is open
          const canSelect = (inRouteMode || inInventoryMode) && node.state === 'Available';

          const handleClick = canSelect
            ? (e: L.LeafletMouseEvent) => {
                L.DomEvent.stopPropagation(e);
                toggleSelectedNode(node.id);
              }
            : undefined;

          return (
            <Marker
              key={node.id}
              position={[node.lat, node.lon]}
              icon={getMarkerIcon(node.state, canSelect, isSelected)}
              title={node.name}
              eventHandlers={handleClick ? { click: handleClick } : undefined}
            />
          );
        })}

        {routeData.polyline.length > 0 && (
          <Polyline positions={routeData.polyline} color="#f97316" weight={5} opacity={0.7} />
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

        {/* Custom origin pin — click to clear */}
        {customOrigin && (
          <Marker
            position={customOrigin}
            icon={getCustomOriginIcon()}
            zIndexOffset={900}
            eventHandlers={{ 
              click: (e) => {
                L.DomEvent.stopPropagation(e);
                setCustomOrigin(null);
              }
            }}
          />
        )}

        {userLocation && (
          <Marker
            position={userLocation}
            zIndexOffset={Z_INDEX.USER_LOCATION}
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
            aria-label="Locate me"
            onClick={locateUser}
            className="p-3 bg-blue-600 hover:bg-blue-500 text-white rounded-xl shadow-lg shadow-blue-900/30 transition-all active:scale-95"
            title="Locate Me"
          >
            <Navigation className="w-5 h-5 fill-white" />
          </button>
        )}

        <div className="bg-[var(--color-surface-hex)]/90 backdrop-blur-md rounded-xl border border-[var(--color-border-strong-hex)] shadow-2xl overflow-hidden flex flex-col">
           <button
              aria-label="Zoom in"
              onClick={() => mapRef.current?.zoomIn()}
              className="p-3 hover:bg-[rgb(var(--color-surface-overlay))] text-[var(--color-text-hex)] transition-colors border-b border-[var(--color-border-hex)]"
           >
             <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
           </button>
           <button
              aria-label="Zoom out"
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
              <Stat label="Distance" value={routeData.distance.toFixed(2)} unit="km" />
              <Stat label="Elev Gain" value={routeData.elevation.toFixed(0)} unit="m" />
            </div>
            {routeData.gpxString && (
              <button onClick={() => downloadGPX(routeData.gpxString!)} className="px-3 py-2 bg-orange-600 hover:bg-orange-500 rounded-lg text-xs font-bold text-white transition-colors flex items-center gap-2">
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
                GPX
              </button>
            )}
        </div>
      )}

      <MapClickConfirmDialog />
    </div>
  );
};

export default MapCanvas;
