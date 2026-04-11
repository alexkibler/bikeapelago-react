import React, { useEffect, useState } from 'react';
import { useGameStore } from '../../store/gameStore';
import { calculateDistance, downloadGPXFromPolyline } from '../../lib/geoUtils';
import { Map, Download, Trash2, Loader2, ChevronDown, ChevronUp, UploadCloud } from 'lucide-react';

const NodeListItem = ({ node, onClick }: { node: any, onClick: () => void }) => {
  return (
    <button
      onClick={onClick}
      className="w-full flex items-center gap-3 px-4 py-3 hover:bg-[rgb(var(--color-surface-overlay))] transition-colors text-left border-b border-[var(--color-border-hex)] last:border-0 group"
    >
      <span className="text-[10px] font-mono text-[var(--color-text-subtle-hex)] w-12 group-hover:text-[var(--color-primary-hex)] transition-colors">
        #{node.ap_location_id || node.id.substring(0, 4)}
      </span>
      <span className="text-sm text-[var(--color-text-muted-hex)] font-medium flex-1 truncate">
        {node.name}
      </span>
    </button>
  );
};

const CategoryHeader = ({ title, count, color, isOpen, onClick }: { title: string, count: number, color: string, isOpen: boolean, onClick: () => void }) => (
  <button
    onClick={onClick}
    className="w-full flex items-center justify-between p-4 bg-[rgb(var(--color-surface-overlay))] hover:bg-[rgb(var(--color-surface-overlay))]/[0.08] transition-colors border-b border-[var(--color-border-hex)]"
  >
    <div className="flex items-center gap-3">
      <div className={`w-2.5 h-2.5 rounded-full`} style={{ backgroundColor: color }}></div>
      <span className="font-bold text-sm tracking-wide text-[var(--color-text-hex)]">
        {title} ({count})
      </span>
    </div>
    {isOpen ? <ChevronUp className="w-4 h-4 text-[var(--color-text-subtle-hex)]" /> : <ChevronDown className="w-4 h-4 text-[var(--color-text-subtle-hex)]" />}
  </button>
);

const RoutePanel = () => {
  const { waypoints, clearWaypoints, setRouteData, routeData, nodes, addWaypoint, addWaypoints, userLocation, togglePanel } = useGameStore();
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

        // TODO: Implement Mapbox routing API integration
        // GraphHopper has been removed as part of the Mapbox migration.
        // This endpoint should call the backend's /route-to-available endpoint instead.
        setError('Routing feature is temporarily disabled during Mapbox migration');
        setLoading(false);
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
    downloadGPXFromPolyline(routeData.polyline);
  };

  const handleRouteToAvailable = () => {
    let availableNodes = nodes.filter(n => n.state === 'Available');
    
    // Sort by proximity to user location if available
    if (userLocation) {
      availableNodes = [...availableNodes].sort((a, b) => {
        const distA = calculateDistance(userLocation[0], userLocation[1], a.lat, a.lon);
        const distB = calculateDistance(userLocation[0], userLocation[1], b.lat, b.lon);
        return distA - distB;
      });
    }

    const points: [number, number][] = availableNodes.map(n => [n.lat, n.lon]);
    
    // If starting fresh and we have a user location, make it the first point
    if (waypoints.length === 0 && userLocation) {
      addWaypoints([userLocation, ...points]);
    } else {
      addWaypoints(points);
    }
  };

  const availableNodes = nodes.filter(n => n.state === 'Available');
  const checkedNodes = nodes.filter(n => n.state === 'Checked');
  const hiddenNodes = nodes.filter(n => n.state === 'Hidden');

  return (
    <div className="flex flex-col h-full bg-[var(--color-surface-hex)] text-[var(--color-text-hex)] overflow-hidden relative">
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-[var(--color-border-hex)] shrink-0">
        <div className="flex items-center gap-2">
          <Map className="w-5 h-5 text-[var(--color-primary-hex)]" />
          <h2 className="text-xl font-black uppercase tracking-tight">Route Builder</h2>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto p-4 space-y-6 pb-32">
        {/* Action Buttons */}
        <div className="space-y-3">
          <button
            onClick={handleRouteToAvailable}
            className="w-full bg-[var(--color-primary-hex)] hover:bg-[var(--color-primary-hover-hex)] text-white font-black py-4 rounded-xl transition-all shadow-lg shadow-primary/20 active:scale-[0.98]"
          >
            Route To Available
          </button>
          <button
            onClick={clearWaypoints}
            className="w-full bg-[rgb(var(--color-surface-overlay))] hover:bg-[rgb(var(--color-surface-overlay))]/[0.08] text-[var(--color-text-muted-hex)] font-bold py-3 rounded-xl transition-all border border-[var(--color-border-hex)]"
          >
            Clear Route
          </button>
          <button
            onClick={() => togglePanel('upload')}
            className="w-full bg-[var(--color-primary-hex)]/10 hover:bg-[var(--color-primary-hex)]/20 text-[var(--color-primary-hex)] font-bold py-3 rounded-xl transition-all border border-[var(--color-primary-hex)]/20 flex items-center justify-center gap-2"
          >
            <UploadCloud className="w-5 h-5" />
            Analyze Ride (.fit)
          </button>
        </div>

        {/* Node Categories */}
        <div className="rounded-xl border border-[var(--color-border-hex)] overflow-hidden bg-[var(--color-surface-hex)]/5">
          <CategoryHeader
            title="Available"
            count={availableNodes.length}
            color="rgb(var(--color-primary))"
            isOpen={openCategories.Available}
            onClick={() => toggleCategory('Available')}
          />
          {openCategories.Available && (
            <div className="max-h-64 overflow-y-auto bg-[var(--color-surface-hex)]/10">
              {availableNodes.map(node => (
                <NodeListItem key={node.id} node={node} onClick={() => addWaypoint([node.lat, node.lon])} />
              ))}
              {availableNodes.length === 0 && <p className="p-4 text-xs text-[var(--color-text-subtle-hex)] italic">No available nodes.</p>}
            </div>
          )}

          <CategoryHeader
            title="Checked"
            count={checkedNodes.length}
            color="rgb(var(--color-success))"
            isOpen={openCategories.Checked}
            onClick={() => toggleCategory('Checked')}
          />
          {openCategories.Checked && (
            <div className="max-h-64 overflow-y-auto bg-[var(--color-surface-hex)]/10">
              {checkedNodes.map(node => (
                <NodeListItem key={node.id} node={node} onClick={() => addWaypoint([node.lat, node.lon])} />
              ))}
              {checkedNodes.length === 0 && <p className="p-4 text-xs text-[var(--color-text-subtle-hex)] italic">No checked nodes.</p>}
            </div>
          )}

          <CategoryHeader
            title="Hidden"
            count={hiddenNodes.length}
            color="rgb(var(--color-border))"
            isOpen={openCategories.Hidden}
            onClick={() => toggleCategory('Hidden')}
          />
          {openCategories.Hidden && (
            <div className="max-h-64 overflow-y-auto bg-[var(--color-surface-hex)]/10">
              {hiddenNodes.map(node => (
                <NodeListItem key={node.id} node={node} onClick={() => addWaypoint([node.lat, node.lon])} />
              ))}
              {hiddenNodes.length === 0 && <p className="p-4 text-xs text-[var(--color-text-subtle-hex)] italic">No hidden nodes.</p>}
            </div>
          )}
        </div>

        {loading && (
          <div className="flex items-center justify-center py-4 text-[var(--color-primary-hex)]">
            <Loader2 className="w-6 h-6 animate-spin" />
          </div>
        )}

        {error && (
          <div className="p-3 bg-[var(--color-error-hex)]/10 border border-[var(--color-error-hex)]/20 rounded-lg text-[var(--color-error-hex)] text-sm">
            {error}
          </div>
        )}
      </div>

      {/* Footer Stats */}
      <div className="p-4 bg-[var(--color-surface-hex)]/90 backdrop-blur-xl border-t border-[var(--color-border-hex)] shrink-0">
        <div className="flex items-center justify-between gap-4">
          <div className="flex gap-6">
            <div className="flex flex-col">
              <span className="text-[10px] font-bold text-[var(--color-text-subtle-hex)] uppercase tracking-widest">Distance</span>
              <span className="text-xl font-black text-[var(--color-text-hex)]">{routeData.distance.toFixed(2)}<span className="text-xs font-normal text-[var(--color-text-subtle-hex)] ml-1">km</span></span>
            </div>
            <div className="flex flex-col">
              <span className="text-[10px] font-bold text-[var(--color-text-subtle-hex)] uppercase tracking-widest">Elev Gain</span>
              <span className="text-xl font-black text-[var(--color-text-hex)]">{routeData.elevation.toFixed(0)}<span className="text-xs font-normal text-[var(--color-text-subtle-hex)] ml-1">m</span></span>
            </div>
          </div>

          <button
            onClick={downloadGPX}
            disabled={!routeData.polyline}
            className={`flex items-center gap-2 px-6 py-3 rounded-xl font-black transition-all ${
              routeData.polyline
              ? 'bg-[var(--color-primary-hex)]/20 text-[var(--color-primary-hex)] hover:bg-[var(--color-primary-hex)]/30 border border-[var(--color-primary-hex)]/30 shadow-lg'
              : 'bg-[rgb(var(--color-surface-overlay))] text-[var(--color-text-subtle-hex)] border border-[var(--color-border-hex)] cursor-not-allowed'
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

