import { useGameStore } from '../../store/gameStore';
import { useArchipelagoStore } from '../../store/archipelagoStore';
import { Package, RefreshCw } from 'lucide-react';

const InventoryPanel = () => {
  const { nodes } = useGameStore();
  const { receivedItems } = useArchipelagoStore();

  // Any item that doesn't correspond to a node is an inventory item
  const nodeLocationIds = new Set(nodes.map(n => n.apLocationId));
  const inventoryItems = receivedItems.filter(item => !nodeLocationIds.has(item.id));

  // Count specific items by name
  const locationSwapCount = inventoryItems.filter(i => i.name === 'Location Swap' || i.name.includes('Swap')).length;

  return (
    <div className="flex flex-col h-full bg-[var(--color-surface-hex)] text-[var(--color-text-hex)] p-4 overflow-y-auto">
      <div className="flex items-center gap-2 mb-6 text-xl font-bold">
        <Package className="w-5 h-5 text-[var(--color-primary-hex)]" />
        <h2 className="panel-title">Inventory</h2>
      </div>

      <div className="space-y-4">
        {/* Dynamic Items List */}
        {locationSwapCount > 0 && (
          <div className="bg-[rgb(var(--color-surface-overlay))] p-4 rounded-xl border border-[var(--color-border-hex)] flex items-center justify-between group hover:border-[var(--color-primary-hex)]/30 transition-all">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-lg bg-[var(--color-primary-hex)]/10 flex items-center justify-center text-[var(--color-primary-hex)]">
                <RefreshCw className="w-5 h-5" />
              </div>
              <div>
                <h3 className="font-bold text-sm">Location Swap</h3>
                <p className="text-[10px] text-[var(--color-text-subtle-hex)] uppercase tracking-widest">Consumable Item</p>
              </div>
            </div>
            <div className="text-2xl font-black text-[var(--color-primary-hex)]">
              {locationSwapCount}
            </div>
          </div>
        )}

        {/* Other Inventory Items */}
        {inventoryItems.filter(i => i.name !== 'Location Swap').map(item => (
           <div key={item.id} className="bg-[rgb(var(--color-surface-overlay))] p-3 rounded-lg border border-[var(--color-border-hex)] flex items-center justify-between">
              <span className="text-xs font-medium text-[var(--color-text-muted-hex)]">{item.name}</span>
              <span className="text-[10px] font-mono text-[var(--color-text-subtle-hex)]">#{item.id}</span>
           </div>
        ))}

        {inventoryItems.length === 0 && (
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
