import { useMemo, useState } from 'react';

import { useArchipelagoStore } from '../../store/archipelagoStore';
import type { GameSession, MapNode } from '../../types/game';

interface GameStatsBarProps {
  session: GameSession | null;
  nodes: MapNode[];
}

const GameStatsBar = ({ session, nodes }: GameStatsBarProps) => {
  const { status, error } = useArchipelagoStore();
  const [showStatsInfo, setShowStatsInfo] = useState(false);

  // ⚡ Bolt: Optimize node counting by using a single O(N) loop and memoizing the result
  const { arrivalChecked, precisionChecked } = useMemo(() => {
    let arrivalCount = 0;
    let precisionCount = 0;
    for (const node of nodes) {
      if (node.is_arrival_checked) arrivalCount++;
      if (node.is_precision_checked) precisionCount++;
    }
    return { arrivalChecked: arrivalCount, precisionChecked: precisionCount };
  }, [nodes]);

  const totalPossibleChecks = nodes.length * 2;

  const statusColor =
    status === 'connected'
      ? 'bg-[var(--color-success-hex)] shadow-[0_0_8px_rgba(var(--color-success),0.5)]'
      : status === 'connecting'
        ? 'bg-[var(--color-warning-hex)] animate-pulse'
        : status === 'error'
          ? 'bg-[var(--color-error-hex)]'
          : 'bg-[var(--color-border-hex)]';

  return (
    <div className='absolute top-0 left-0 right-0 bg-[var(--color-surface-hex)]/90 backdrop-blur-md border-b border-[var(--color-border-strong-hex)] px-4 py-2 flex items-center justify-between h-12 z-[1000]'>
      {/* Click away layer to close statistics popover */}
      {showStatsInfo && (
        <div
          className='fixed inset-0 z-[1005]'
          onClick={() => setShowStatsInfo(false)}
          aria-hidden='true'
        />
      )}
      <div className='flex items-center gap-2 px-3 py-1.5 rounded-lg border border-[var(--color-border-hex)] bg-[rgb(var(--color-surface-overlay))] relative group min-w-0'>
        <div
          className={`w-2 h-2 rounded-full flex-shrink-0 ${statusColor}`}
        ></div>
        <div className='flex flex-col min-w-0'>
          <span className='text-[10px] font-bold text-[var(--color-text-muted-hex)] truncate leading-none mb-1'>
            {session?.name || session?.ap_seed_name || 'Unnamed Session'}
          </span>
          <div className='flex items-center gap-2'>
            <span className='text-[10px] font-medium text-[var(--color-text-subtle-hex)] truncate leading-none'>
              {session?.ap_slot_name || 'Local Rider'}
            </span>
            {session?.progression_mode === 'quadrant' && (
              <div className='flex gap-0.5'>
                <div
                  className={`w-1.5 h-1.5 rounded-full ${session.north_pass_received ? 'bg-blue-500' : 'bg-gray-700'}`}
                  title='North'
                ></div>
                <div
                  className={`w-1.5 h-1.5 rounded-full ${session.east_pass_received ? 'bg-blue-500' : 'bg-gray-700'}`}
                  title='East'
                ></div>
                <div
                  className={`w-1.5 h-1.5 rounded-full ${session.south_pass_received ? 'bg-blue-500' : 'bg-gray-700'}`}
                  title='South'
                ></div>
                <div
                  className={`w-1.5 h-1.5 rounded-full ${session.west_pass_received ? 'bg-blue-500' : 'bg-gray-700'}`}
                  title='West'
                ></div>
              </div>
            )}
            {session?.progression_mode === 'radius' && (
              <span className='text-[10px] text-orange-500 font-bold leading-none'>
                R{session.radius_step}
              </span>
            )}
          </div>
        </div>
        {error && (
          <div className='absolute top-full left-0 mt-2 p-2 bg-[var(--color-error-hex)]/90 border border-[var(--color-error-hex)] rounded text-[10px] text-[var(--color-text-hex)] opacity-0 group-hover:opacity-100 transition-opacity z-50 w-48'>
            {error}
          </div>
        )}
      </div>

      {session && session.macguffins_required > 0 && (
        <div
          className='flex items-center gap-2 px-3 py-1.5 rounded-lg border border-[var(--color-border-hex)] bg-[rgb(var(--color-surface-overlay))]'
          title='Macguffins collected'
        >
          <span className='text-yellow-400 text-sm leading-none'>&#10022;</span>
          <span className='text-xs font-black text-[var(--color-text-hex)] leading-none tabular-nums'>
            {session.macguffins_collected}{' '}
            <span className='font-normal text-[var(--color-text-subtle-hex)]'>
              / {session.macguffins_required}
            </span>
          </span>
        </div>
      )}

      <div className='relative'>
        <button
          onClick={() => setShowStatsInfo(!showStatsInfo)}
          className='flex items-center gap-4 font-black text-xs uppercase tracking-tight ml-4 p-1.5 rounded-lg hover:bg-[rgb(var(--color-surface-overlay))] transition-colors focus:outline-none focus:ring-2 focus:ring-[var(--color-border-hex)]'
          aria-label='Toggle node statistics'
          aria-expanded={showStatsInfo}
        >
          <div className='flex items-baseline gap-1' title='Arrival Checks'>
            <span className='text-[var(--color-primary-hex)] leading-none text-sm'>
              {arrivalChecked}
            </span>
            <span className='text-[10px] text-[var(--color-text-subtle-hex)]'>
              / {nodes.length}
            </span>
          </div>
          <div className='flex items-baseline gap-1' title='Precision Checks'>
            <span className='text-[var(--color-success-hex)] leading-none text-sm'>
              {precisionChecked}
            </span>
            <span className='text-[10px] text-[var(--color-text-subtle-hex)]'>
              / {nodes.length}
            </span>
          </div>
        </button>

        {showStatsInfo && (
          <div className='absolute top-full right-0 mt-3 w-48 bg-[var(--color-surface-hex)] border border-[var(--color-border-strong-hex)] rounded-xl shadow-xl overflow-hidden z-[1010]'>
            <div className='flex flex-col'>
              <div className='flex items-center justify-between px-4 py-3 border-b border-[var(--color-border-hex)]/50'>
                <div className='flex items-center gap-2'>
                  <div className='w-2.5 h-2.5 rounded-full bg-[var(--color-primary-hex)]'></div>
                  <span className='text-[10px] font-bold text-[var(--color-text-muted-hex)] tracking-wider uppercase'>
                    Arrival (100m)
                  </span>
                </div>
                <span className='text-[var(--color-primary-hex)] font-bold'>
                  {arrivalChecked}
                </span>
              </div>
              <div className='flex items-center justify-between px-4 py-3 border-b border-[var(--color-border-hex)]/50'>
                <div className='flex items-center gap-2'>
                  <div className='w-2.5 h-2.5 rounded-full bg-[var(--color-success-hex)]'></div>
                  <span className='text-[10px] font-bold text-[var(--color-text-muted-hex)] tracking-wider uppercase'>
                    Precision (25m)
                  </span>
                </div>
                <span className='text-[var(--color-success-hex)] font-bold'>
                  {precisionChecked}
                </span>
              </div>
              <div className='px-4 py-2 bg-[var(--color-surface-alt-hex)]'>
                <div className='flex justify-between items-center mb-1'>
                  <span className='text-[8px] font-bold text-[var(--color-text-subtle-hex)] uppercase tracking-widest'>
                    Total Seed Progress
                  </span>
                  <span className='text-[10px] font-mono text-[var(--color-text-muted-hex)]'>
                    {Math.round(
                      ((arrivalChecked + precisionChecked) /
                        totalPossibleChecks) *
                        100,
                    )}
                    %
                  </span>
                </div>
                <div className='w-full bg-gray-800 rounded-full h-1'>
                  <div
                    className='bg-[var(--color-success-hex)] h-1 rounded-full'
                    style={{
                      width: `${((arrivalChecked + precisionChecked) / totalPossibleChecks) * 100}%`,
                    }}
                  ></div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default GameStatsBar;
