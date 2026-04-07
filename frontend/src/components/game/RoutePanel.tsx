import React, { useEffect, useState } from 'react';
import { useGameStore } from '../../store/gameStore';
import { getGraphhopperUrl } from '../../lib/graphhopper';
import { Map, Download, Trash2, Loader2, ChevronDown, ChevronUp } from 'lucide-react';

const NodeListItem = ({ node, onClick }: { node: any, onClick: () => void }) => {
  return (
    <button 
      onClick={onClick}
      className="w-full flex items-center gap-3 px-4 py-3 hover:bg-white/5 transition-colors text-left border-b border-white/5 last:border-0 group"
    >
      <span className="text-[10px] font-mono text-neutral-500 w-12 group-hover:text-orange-500 transition-colors">
        #{node.ap_location_id || node.id.substring(0, 4)}
      </span>
      <span className="text-sm text-neutral-300 font-medium flex-1 truncate">
        {node.name}
      </span>
    </button>
  );
};

const CategoryHeader = ({ title, count, color, isOpen, onClick }: { title: string, count: number, color: string, isOpen: boolean, onClick: () => void }) => (
  <button 
    onClick={onClick}
    className="w-full flex items-center justify-between p-4 bg-white/5 hover:bg-white/[0.08] transition-colors border-b border-white/10"
  >
    <div className="flex items-center gap-3">
      <div className={`w-2.5 h-2.5 rounded-full`} style={{ backgroundColor: color }}></div>
      <span className="font-bold text-sm tracking-wide">
        {title} ({count})
      </span>
    </div>
    {isOpen ? <ChevronUp className="w-4 h-4 text-neutral-500" /> : <ChevronDown className="w-4 h-4 text-neutral-500" />}
  </button>
);

const RoutePanel = () => {
  const { waypoints, clearWaypoints, setRouteData, routeData, nodes, addWaypoint, addWaypoints } = useGameStore();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  const [openCategories, setOpenCategories] = useState({
    Available: true,
    Checked: false,
    Hidden: false
  });

  const toggleCategory = (cat: string) => {
    setOpenCategories(prev => ({ ...prev, [cat]: !prev[cat as keyof typeof prev] }));
  };

  useEffect(() => {
    const fetchRoute = async () => {
      if (waypoints.length < 2) {
        setRouteData({ distance: 0, elevation: 0, polyline: null });
        return;
      }

      setLoading(true);
      setError(null);
      try {
        if ((window as any).PLAYWRIGHT_TEST) {
          setRouteData({
            distance: waypoints.length * 1.5,
            elevation: waypoints.length * 10,
            polyline: JSON.stringify(waypoints.map(wp => [wp[1], wp[0], 250]))
          });
          setLoading(false);
          return;
        }

        const ghUrl = `${getGraphhopperUrl()}/route`;
        const points = waypoints.map(wp => [wp[1], wp[0]]);
        
        const response = await fetch(ghUrl, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            points: points,
            profile: 'bike',
            locale: 'en',
            points_encoded: false,
            elevation: true
          })
        });

        if (!response.ok) throw new Error(`Routing failed with status ${response.status}`);
        const data = await response.json() as { paths: Array<{ distance: number, ascend: number, points: { coordinates: number[][] } }> };
        
        const path = data.paths[0];
        setRouteData({
          distance: path.distance / 1000,
          elevation: path.ascend || 0,
          polyline: JSON.stringify(path.points.coordinates)
        });
      } catch (err: unknown) {
        setError(err instanceof Error ? err.message : 'Routing failed');
      } finally {
        setLoading(false);
      }
    };

    fetchRoute();
  }, [waypoints, setRouteData]);

  const downloadGPX = () => {
    if (!routeData.polyline) return;
    
    const coordinates = JSON.parse(routeData.polyline) as [number, number, number?][];
    let gpx = `<?xml version="1.0" encoding="UTF-8"?>
<gpx version="1.1" creator="Bikeapelago" xmlns="http://www.topografix.com/GPX/1/1">
  <trk>
    <name>Bikeapelago Route</name>
    <trkseg>`;
    
    coordinates.forEach((coord) => {
      gpx += `
      <trkpt lat="${coord[1]}" lon="${coord[0]}">
        <ele>${coord[2] || 0}</ele>
      </trkpt>`;
    });
    
    gpx += `
    </trkseg>
  </trk>
</gpx>`;

    const blob = new Blob([gpx], { type: 'application/gpx+xml' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'bikeapelago_route.gpx';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  const handleRouteToAvailable = () => {
    const availableNodes = nodes.filter(n => n.state === 'Available');
    const points: [number, number][] = availableNodes.map(n => [n.lat, n.lon]);
    addWaypoints(points);
  };

  const availableNodes = nodes.filter(n => n.state === 'Available');
  const checkedNodes = nodes.filter(n => n.state === 'Checked');
  const hiddenNodes = nodes.filter(n => n.state === 'Hidden');

  return (
    <div className="flex flex-col h-full bg-neutral-900 text-white overflow-hidden relative">
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-white/10 shrink-0">
        <div className="flex items-center gap-2">
          <Map className="w-5 h-5 text-orange-500" />
          <h2 className="text-xl font-black uppercase tracking-tight">Route Builder</h2>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto p-4 space-y-6 pb-32">
        {/* Action Buttons */}
        <div className="space-y-3">
          <button 
            onClick={handleRouteToAvailable}
            className="w-full bg-orange-600 hover:bg-orange-500 text-white font-black py-4 rounded-xl transition-all shadow-lg shadow-orange-900/20 active:scale-[0.98]"
          >
            Route To Available
          </button>
          <button 
            onClick={clearWaypoints}
            className="w-full bg-white/5 hover:bg-white/10 text-neutral-300 font-bold py-3 rounded-xl transition-all border border-white/10"
          >
            Clear Route
          </button>
        </div>

        {/* Node Categories */}
        <div className="rounded-xl border border-white/10 overflow-hidden bg-neutral-950/20">
          <CategoryHeader 
            title="Available" 
            count={availableNodes.length} 
            color="#f97316" 
            isOpen={openCategories.Available}
            onClick={() => toggleCategory('Available')}
          />
          {openCategories.Available && (
            <div className="max-h-64 overflow-y-auto bg-neutral-950/40">
              {availableNodes.map(node => (
                <NodeListItem key={node.id} node={node} onClick={() => addWaypoint([node.lat, node.lon])} />
              ))}
              {availableNodes.length === 0 && <p className="p-4 text-xs text-neutral-600 italic">No available nodes.</p>}
            </div>
          )}

          <CategoryHeader 
            title="Checked" 
            count={checkedNodes.length} 
            color="#22c55e" 
            isOpen={openCategories.Checked}
            onClick={() => toggleCategory('Checked')}
          />
          {openCategories.Checked && (
            <div className="max-h-64 overflow-y-auto bg-neutral-950/40">
              {checkedNodes.map(node => (
                <NodeListItem key={node.id} node={node} onClick={() => addWaypoint([node.lat, node.lon])} />
              ))}
              {checkedNodes.length === 0 && <p className="p-4 text-xs text-neutral-600 italic">No checked nodes.</p>}
            </div>
          )}

          <CategoryHeader 
            title="Hidden" 
            count={hiddenNodes.length} 
            color="#525252" 
            isOpen={openCategories.Hidden}
            onClick={() => toggleCategory('Hidden')}
          />
          {openCategories.Hidden && (
            <div className="max-h-64 overflow-y-auto bg-neutral-950/40">
              {hiddenNodes.map(node => (
                <NodeListItem key={node.id} node={node} onClick={() => addWaypoint([node.lat, node.lon])} />
              ))}
              {hiddenNodes.length === 0 && <p className="p-4 text-xs text-neutral-600 italic">No hidden nodes.</p>}
            </div>
          )}
        </div>

        {loading && (
          <div className="flex items-center justify-center py-4 text-orange-500">
            <Loader2 className="w-6 h-6 animate-spin" />
          </div>
        )}

        {error && (
          <div className="p-3 bg-red-500/10 border border-red-500/20 rounded-lg text-red-500 text-sm">
            {error}
          </div>
        )}
      </div>

      {/* Footer Stats - Floating Style */}
      <div className="absolute bottom-0 left-0 right-0 p-4 bg-neutral-900/90 backdrop-blur-xl border-t border-white/10 z-20">
        <div className="flex items-center justify-between gap-4">
          <div className="flex gap-6">
            <div className="flex flex-col">
              <span className="text-[10px] font-bold text-neutral-500 uppercase tracking-widest">Distance</span>
              <span className="text-xl font-black text-white">{routeData.distance.toFixed(2)}<span className="text-xs font-normal text-neutral-500 ml-1">km</span></span>
            </div>
            <div className="flex flex-col">
              <span className="text-[10px] font-bold text-neutral-500 uppercase tracking-widest">Elev Gain</span>
              <span className="text-xl font-black text-white">{routeData.elevation.toFixed(0)}<span className="text-xs font-normal text-neutral-500 ml-1">m</span></span>
            </div>
          </div>

          <button 
            onClick={downloadGPX}
            disabled={!routeData.polyline}
            className={`flex items-center gap-2 px-6 py-3 rounded-xl font-black transition-all ${
              routeData.polyline 
              ? 'bg-[#4a3a2a] text-orange-500 hover:bg-[#5a4a3a] border border-orange-900/30 shadow-lg' 
              : 'bg-white/5 text-neutral-600 border border-white/5 cursor-not-allowed'
            }`}
          >
            <Download className="w-4 h-4" />
            <span className="hidden sm:inline">Download GPX</span>
            <span className="sm:hidden">GPX</span>
          </button>
        </div>
      </div>
    </div>
  );
};

export default RoutePanel;

