import { useState, KeyboardEvent } from 'react';
import { useParams } from 'react-router-dom';
import { useGameStore } from '../../store/gameStore';
import { useArchipelagoStore } from '../../store/archipelagoStore';
import { useDebugStore } from '../../store/debugStore';
import { Package, RefreshCw, Crosshair, Radio, CheckCircle2 } from 'lucide-react';
import { apiFetch, ENDPOINTS } from '../../lib/api';
import { useToast } from '../../hooks/useToast';

const InventoryPanel = () => {
  const { id: sessionId } = useParams<{ id: string }>();
  const session = useGameStore(s => s.session);
  const { selectedNodeIds, triggerSync, clearSelectedNodes } = useGameStore();
  const { receivedItems } = useArchipelagoStore();
  const toast = useToast();
  const [isUsing, setIsUsing] = useState(false);
  const debugMode = useDebugStore(s => s.debugMode);

  // Define useful item names and IDs
  const ITEMS = {
    DETOUR: { name: 'Detour', id: 802010 },
    DRONE: { name: 'Drone', id: 802011 },
    SIGNAL_AMPLIFIER: { name: 'Signal Amplifier', id: 802012 },
    PASS_NORTH: { name: 'North Quadrant Pass', id: 802002 },
    PASS_SOUTH: { name: 'South Quadrant Pass', id: 802003 },
    PASS_EAST: { name: 'East Quadrant Pass', id: 802004 },
    PASS_WEST: { name: 'West Quadrant Pass', id: 802005 },
    RADIUS_INC: { name: 'Progressive Radius Increase', id: 802006 },
  };

  // Count items
  const detourCount = receivedItems.filter(i => i.name === ITEMS.DETOUR.name || i.name === 'Location Swap').length;
  const droneCount = receivedItems.filter(i => i.name === ITEMS.DRONE.name).length;
  const signalAmpCount = receivedItems.filter(i => i.name === ITEMS.SIGNAL_AMPLIFIER.name).length;

  const handleUseDetour = async () => {
    if (!sessionId || selectedNodeIds.size !== 1) {
      toast.error('Select exactly one node to detour');
      return;
    }
    const nodeId = Array.from(selectedNodeIds)[0];
    setIsUsing(true);
    try {
      await apiFetch(`${ENDPOINTS.ITEMS.DETOUR(sessionId)}?nodeId=${nodeId}`, { method: 'POST' });
      toast.success('Node relocated!');
      triggerSync();
      clearSelectedNodes();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to use Detour';
      toast.error(message);
    } finally {
      setIsUsing(false);
    }
  };

  const handleUseDrone = async () => {
    if (!sessionId || selectedNodeIds.size !== 1) {
      toast.error('Select exactly one node for the drone');
      return;
    }
    const nodeId = Array.from(selectedNodeIds)[0];
    setIsUsing(true);
    try {
      await apiFetch(`${ENDPOINTS.ITEMS.DRONE(sessionId)}?nodeId=${nodeId}`, { method: 'POST' });
      toast.success('Node completed by drone!');
      triggerSync();
      clearSelectedNodes();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to use Drone';
      toast.error(message);
    } finally {
      setIsUsing(false);
    }
  };

  const handleUseSignalAmplifier = async () => {
    if (!sessionId) return;
    setIsUsing(true);
    try {
      await apiFetch(ENDPOINTS.ITEMS.SIGNAL_AMPLIFIER(sessionId), { method: 'POST' });
      toast.success('Signal Amplifier activated for next ride!');
      triggerSync();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to activate Signal Amplifier';
      toast.error(message);
    } finally {
      setIsUsing(false);
    }
  };

  const handleDebugUpdate = async (itemId: number, countStr: string) => {
    if (!sessionId) return;
    const count = parseInt(countStr);
    if (isNaN(count)) return;

    try {
      await apiFetch(`${ENDPOINTS.ITEMS.DEBUG_SET_ITEM_COUNT(sessionId)}?itemId=${itemId}&count=${count}`, { method: 'POST' });
      triggerSync();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Debug update failed';
      toast.error(message);
    }
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>, itemId: number) => {
    if (e.key === 'Enter') {
      handleDebugUpdate(itemId, e.currentTarget.value);
      e.currentTarget.blur();
    }
  };

  const otherItems = Object.values(
    receivedItems
      .filter(i => ![ITEMS.DETOUR.name, ITEMS.DRONE.name, ITEMS.SIGNAL_AMPLIFIER.name, 'Location Swap'].includes(i.name))
      .reduce((acc, item) => {
        if (!acc[item.id]) {
          acc[item.id] = { ...item, count: 0 };
        }
        acc[item.id].count += 1;
        return acc;
      }, {} as Record<number, { id: number, name: string, count: number }>)
  );

  const ItemCount = ({ count, itemId }: { count: number, itemId: number }) => {
    if (debugMode) {
      return (
        <input
          type="number"
          defaultValue={count}
          onBlur={(e) => handleDebugUpdate(itemId, e.target.value)}
          onKeyDown={(e) => handleKeyDown(e, itemId)}
          className="w-12 bg-[var(--color-surface-hex)] border border-[var(--color-border-hex)] rounded text-right px-1 font-mono text-sm focus:outline-none focus:ring-1 focus:ring-orange-500"
        />
      );
    }
    return <div className="text-xl font-black text-current opacity-80">{count}</div>;
  };

  return (
    <div className="flex flex-col h-full bg-[var(--color-surface-hex)] text-[var(--color-text-hex)] p-4 overflow-y-auto">
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-2 text-xl font-bold">
          <Package className="w-5 h-5 text-[var(--color-primary-hex)]" />
          <h2 className="panel-title">Inventory</h2>
        </div>
      </div>

      <div className="space-y-4">
        {/* Detour */}
        <div className="bg-[rgb(var(--color-surface-overlay))] p-4 rounded-xl border border-[var(--color-border-strong-hex)] shadow-sm transition-all">
          <div className="flex items-center justify-between mb-3 text-blue-500">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-lg bg-blue-500/10 flex items-center justify-center border border-blue-500/20">
                <RefreshCw className="w-5 h-5" />
              </div>
              <div>
                <h3 className="font-bold text-sm text-[var(--color-text-hex)]">The Detour</h3>
                <p className="text-[10px] text-[var(--color-text-muted-hex)] font-bold uppercase tracking-widest">Relocate Node</p>
              </div>
            </div>
            <ItemCount count={detourCount} itemId={ITEMS.DETOUR.id} />
          </div>
          {detourCount > 0 && (
            <button 
              onClick={handleUseDetour}
              disabled={isUsing || selectedNodeIds.size !== 1}
              className="w-full py-2 bg-blue-500 hover:bg-blue-600 disabled:bg-gray-600 text-white text-xs font-bold rounded-lg transition-colors flex items-center justify-center gap-2"
            >
              {selectedNodeIds.size === 1 ? 'Use on Selected Node' : 'Select a node on map'}
            </button>
          )}
        </div>

        {/* Drone */}
        <div className="bg-[rgb(var(--color-surface-overlay))] p-4 rounded-xl border border-[var(--color-border-strong-hex)] shadow-sm transition-all">
          <div className="flex items-center justify-between mb-3 text-purple-500">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-lg bg-purple-500/10 flex items-center justify-center border border-purple-500/20">
                <Crosshair className="w-5 h-5" />
              </div>
              <div>
                <h3 className="font-bold text-sm text-[var(--color-text-hex)]">The Drone</h3>
                <p className="text-[10px] text-[var(--color-text-muted-hex)] font-bold uppercase tracking-widest">Instant Completion</p>
              </div>
            </div>
            <ItemCount count={droneCount} itemId={ITEMS.DRONE.id} />
          </div>
          {droneCount > 0 && (
            <button 
              onClick={handleUseDrone}
              disabled={isUsing || selectedNodeIds.size !== 1}
              className="w-full py-2 bg-purple-500 hover:bg-purple-600 disabled:bg-gray-600 text-white text-xs font-bold rounded-lg transition-colors flex items-center justify-center gap-2"
            >
              {selectedNodeIds.size === 1 ? 'Send Drone' : 'Select a node on map'}
            </button>
          )}
        </div>

        {/* Signal Amplifier */}
        <div className="bg-[rgb(var(--color-surface-overlay))] p-4 rounded-xl border border-[var(--color-border-strong-hex)] shadow-sm transition-all">
          <div className="flex items-center justify-between mb-3 text-orange-500">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-lg bg-orange-500/10 flex items-center justify-center border border-orange-500/20">
                <Radio className="w-5 h-5" />
              </div>
              <div>
                <h3 className="font-bold text-sm text-[var(--color-text-hex)]">Signal Amplifier</h3>
                <p className="text-[10px] text-[var(--color-text-muted-hex)] font-bold uppercase tracking-widest">2x Radius Buff</p>
              </div>
            </div>
            <ItemCount count={signalAmpCount} itemId={ITEMS.SIGNAL_AMPLIFIER.id} />
          </div>
          {signalAmpCount > 0 && (
            <button 
              onClick={handleUseSignalAmplifier}
              disabled={isUsing}
              className="w-full py-2 bg-orange-500 hover:bg-orange-600 disabled:bg-gray-600 text-white text-xs font-bold rounded-lg transition-colors"
            >
              Activate Amplifier
            </button>
          )}
        </div>

        {/* Debug Progression Items */}
        {debugMode && (
          <div className="mt-8 space-y-2">
            <h4 className="text-[10px] uppercase tracking-[0.2em] text-orange-500 mb-4 font-black">Debug Progression</h4>
            {[ITEMS.PASS_NORTH, ITEMS.PASS_EAST, ITEMS.PASS_SOUTH, ITEMS.PASS_WEST, ITEMS.RADIUS_INC].map(item => (
              <div key={item.id} className="bg-[rgb(var(--color-surface-overlay))] p-3 rounded-lg border border-[var(--color-border-hex)] flex items-center justify-between">
                <span className="text-xs font-bold text-[var(--color-text-muted-hex)]">{item.name}</span>
                <ItemCount count={receivedItems.filter(i => i.name === item.name).length} itemId={item.id} />
              </div>
            ))}
          </div>
        )}

        {/* Other Items */}
        {!debugMode && otherItems.length > 0 && (
          <div className="mt-8">
            <h4 className="text-[10px] uppercase tracking-[0.2em] text-[var(--color-text-subtle-hex)] mb-4 font-bold">Progression & Filler</h4>
            <div className="grid grid-cols-1 gap-2">
              {otherItems.map((item) => (
                <div key={item.id} className="bg-[rgb(var(--color-surface-overlay))] p-3 rounded-lg border border-[var(--color-border-hex)] flex items-center justify-between">
                  <span className="text-xs font-medium text-[var(--color-text-muted-hex)]">
                    {item.name} {item.count > 1 && <span className="opacity-60 ml-1">(x{item.count})</span>}
                  </span>
                  <CheckCircle2 className="w-3 h-3 text-green-500/50" />
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default InventoryPanel;
