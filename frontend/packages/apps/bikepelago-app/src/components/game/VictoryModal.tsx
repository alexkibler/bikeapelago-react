import { useEffect, useRef } from 'react';

import confetti from 'canvas-confetti';
import { useNavigate } from 'react-router-dom';

import type { GameSession } from '../../types/game';

interface VictoryModalProps {
  session: GameSession;
}

const VictoryModal = ({ session }: VictoryModalProps) => {
  const navigate = useNavigate();
  const firedRef = useRef(false);

  useEffect(() => {
    if (firedRef.current) return;
    firedRef.current = true;

    const duration = 6000;
    const end = Date.now() + duration;

    const frame = () => {
      void confetti({
        particleCount: 3,
        angle: 60,
        spread: 55,
        origin: { x: 0 },
        colors: ['#f97316', '#facc15', '#22c55e', '#3b82f6', '#a855f7'],
      });
      void confetti({
        particleCount: 3,
        angle: 120,
        spread: 55,
        origin: { x: 1 },
        colors: ['#f97316', '#facc15', '#22c55e', '#3b82f6', '#a855f7'],
      });

      if (Date.now() < end) {
        requestAnimationFrame(frame);
      }
    };

    frame();
  }, []);

  return (
    <div className='fixed inset-0 z-[2000] flex items-center justify-center bg-black/70 backdrop-blur-sm p-4'>
      <div className='relative bg-[var(--color-surface-hex)] border border-[var(--color-border-strong-hex)] rounded-2xl shadow-2xl max-w-sm w-full text-center p-8 flex flex-col items-center gap-5'>
        <div className='text-6xl leading-none'>&#127881;</div>

        <div>
          <h1 className='text-2xl font-black text-[var(--color-text-hex)] leading-tight'>
            You Win!
          </h1>
          <p className='text-sm text-[var(--color-text-subtle-hex)] mt-1'>
            {session.name || session.ap_seed_name || 'Session'} complete
          </p>
        </div>

        <div className='flex items-center gap-3 bg-yellow-500/10 border border-yellow-500/30 rounded-xl px-5 py-3'>
          <span className='text-yellow-400 text-2xl leading-none'>
            &#10022;
          </span>
          <div className='text-left'>
            <div className='text-xs font-bold text-[var(--color-text-muted-hex)] uppercase tracking-widest'>
              Macguffins
            </div>
            <div className='text-xl font-black text-yellow-400 leading-tight'>
              {session.macguffins_collected}{' '}
              <span className='text-sm font-normal text-[var(--color-text-subtle-hex)]'>
                / {session.macguffins_required}
              </span>
            </div>
          </div>
        </div>

        <button
          onClick={() => void navigate('/')}
          className='w-full py-3 rounded-xl font-bold text-sm bg-[var(--color-primary-hex)] text-white hover:opacity-90 transition-opacity'
        >
          Back to Sessions
        </button>
      </div>
    </div>
  );
};

export default VictoryModal;
