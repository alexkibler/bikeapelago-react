import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useGameStore } from '../../store/gameStore';
import { useArchipelagoStore } from '../../store/archipelagoStore';
import { Package, RefreshCw, Crosshair, Radio, CheckCircle2 } from 'lucide-react';
import { apiFetch, ENDPOINTS } from '../../lib/api';
import { useToast } from '../../hooks/useToast';

const InventoryPanel = () => {
  const { id: sessionId } = useParams<{ id: string }>();
  const { selectedNodeIds, triggerSync, clearSelectedNodes } = useGameStore();
  const { receivedItems } = useArchipelagoStore();
  const toast = useToast();
  const [isUsing, setIsUsing] = useState(false);

  // Define useful item names
  const ITEM_DETOUR = 'Detour';
  const ITEM_DRONE = 'Drone';
  const ITEM_SIGNAL_AMPLIFIER = 'Signal Amplifier';

  // Count items
  const detourCount = receivedItems.filter(i => i.name === ITEM_DETOUR || i.name === 'Location Swap').length;
  const droneCount = receivedItems.filter(i => i.name === ITEM_DRONE).length;
  const signalAmpCount = receivedItems.filter(i => i.name === ITEM_SIGNAL_AMPLIFIER).length;

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

  const otherItems = receivedItems.filter(i => ![ITEM_DETOUR, ITEM_DRONE, ITEM_SIGNAL_AMPLIFIER, 'Location Swap'].includes(i.name));

  return (
    <div className="flex flex-col h-full bg-[var(--color-surface-hex)] text-[var(--color-text-hex)] p-4 overflow-y-auto">
      <div className="flex items-center gap-2 mb-6 text-xl font-bold">
        <Package className="w-5 h-5 text-[var(--color-primary-hex)]" />
        <h2 className="panel-title">Inventory</h2>
      </div>

      <div className="space-y-4">
        {/* Detour */}
        <div className={`bg-[rgb(var(--color-surface-overlay))] p-4 rounded-xl border border-[var(--color-border-hex)] transition-all ${detourCount > 0 ? 'opacity-100' : 'opacity-40'}`}>
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-lg bg-blue-500/10 flex items-center justify-center text-blue-500">
                <RefreshCw className="w-5 h-5" />
              </div>
              <div>
                <h3 className="font-bold text-sm">The Detour</h3>
                <p className="text-[10px] text-[var(--color-text-subtle-hex)] uppercase tracking-widest">Relocate Node</p>
              </div>
            </div>
            <div className="text-xl font-black text-blue-500">{detourCount}</div>
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
        <div className={`bg-[rgb(var(--color-surface-overlay))] p-4 rounded-xl border border-[var(--color-border-hex)] transition-all ${droneCount > 0 ? 'opacity-100' : 'opacity-40'}`}>
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-lg bg-purple-500/10 flex items-center justify-center text-purple-500">
                <Crosshair className="w-5 h-5" />
              </div>
              <div>
                <h3 className="font-bold text-sm">The Drone</h3>
                <p className="text-[10px] text-[var(--color-text-subtle-hex)] uppercase tracking-widest">Instant Completion</p>
              </div>
            </div>
            <div className="text-xl font-black text-purple-500">{droneCount}</div>
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
        <div className={`bg-[rgb(var(--color-surface-overlay))] p-4 rounded-xl border border-[var(--color-border-hex)] transition-all ${signalAmpCount > 0 ? 'opacity-100' : 'opacity-40'}`}>
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-lg bg-orange-500/10 flex items-center justify-center text-orange-500">
                <Radio className="w-5 h-5" />
              </div>
              <div>
                <h3 className="font-bold text-sm">Signal Amplifier</h3>
                <p className="text-[10px] text-[var(--color-text-subtle-hex)] uppercase tracking-widest">2x Radius Buff</p>
              </div>
            </div>
            <div className="text-xl font-black text-orange-500">{signalAmpCount}</div>
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

        {/* Other Items */}
        {otherItems.length > 0 && (
          <div className="mt-8">
            <h4 className="text-[10px] uppercase tracking-[0.2em] text-[var(--color-text-subtle-hex)] mb-4 font-bold">Progression & Filler</h4>
            <div className="grid grid-cols-1 gap-2">
              {otherItems.map((item, idx) => (
                <div key={`${item.id}-${idx}`} className="bg-[rgb(var(--color-surface-overlay))] p-3 rounded-lg border border-[var(--color-border-hex)] flex items-center justify-between">
                  <span className="text-xs font-medium text-[var(--color-text-muted-hex)]">{item.name}</span>
                  <CheckCircle2 className="w-3 h-3 text-green-500/50" />
                </div>
              ))}
            </div>
          </div>
        )}

        {receivedItems.length === 0 && (
          <div className="text-center py-12 bg-[var(--color-surface-hex)]/50 rounded-2xl border border-[var(--color-border-hex)] border-dashed">
            <Package className="w-12 h-12 text-[var(--color-text-subtle-hex)] mx-auto mb-4 opacity-20" />
            <p className="text-[var(--color-text-subtle-hex)] text-sm">Your inventory is empty.</p>
            <p className="text-[10px] text-[var(--color-text-subtle-hex)] mt-1 uppercase tracking-widest">Find items by checking locations</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default InventoryPanel;
