import { useState } from 'react';
import { useArchipelagoStore } from '../../store/archipelagoStore';
import type { GameSession, MapNode } from '../../types/game';

interface GameStatsBarProps {
  session: GameSession | null;
  nodes: MapNode[];
}

const GameStatsBar = ({ session, nodes }: GameStatsBarProps) => {
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
      {/* Click away layer to close statistics popover */}
      {showStatsInfo && (
        <div 
          className="fixed inset-0 z-[1005]" 
          onClick={() => setShowStatsInfo(false)}
          aria-hidden="true"
        />
      )}
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
          </div>
        )}
      </div>
    </div>
  );
};

export default GameStatsBar;
