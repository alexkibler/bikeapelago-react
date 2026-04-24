import type { ReactElement } from 'react';
import { Bike, Map, Route } from 'lucide-react';
import { Link } from 'react-router-dom';

export default function About(): ReactElement {
  return (
    <div className='min-h-[80vh] flex items-center justify-center p-6'>
      <div className='w-full max-w-2xl'>
        <div className='text-center mb-10'>
          <div className='inline-flex items-center justify-center w-16 h-16 rounded-3xl bg-orange-600/10 border border-orange-500/20 mb-6'>
            <Bike className='w-8 h-8 text-orange-500' />
          </div>
          <h1 className='text-4xl font-black text-[var(--color-text-hex)] italic uppercase tracking-tighter mb-2'>
            Bikeapelago
          </h1>
        </div>

        <div className='space-y-4 mb-10'>
          <div className='flex gap-4 p-5 bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] rounded-2xl'>
            <div className='flex-shrink-0 w-10 h-10 rounded-xl bg-orange-500/10 border border-orange-500/20 flex items-center justify-center'>
              <Map className='w-5 h-5 text-orange-500' />
            </div>
            <div>
              <h2 className='text-[var(--color-text-hex)] font-bold mb-1'>Explore the Archipelago</h2>
              <p className='text-[var(--color-text-subtle-hex)] text-sm leading-relaxed'>
                Bikeapelago (proper title TBD) is a real world cycling/walking route planner built
                around the concept of{' '}
                <a
                  href='https://archipelago.gg'
                  target='_blank'
                  rel='noopener noreferrer'
                  className='text-orange-500 hover:text-orange-600 font-bold transition-colors'
                >
                  Archipelago
                </a>{' '}
                multi-world randomizers. Explore the world around you and collect MacGuffins and
                unlock items for yourself and other players in your seed or solo.
              </p>
            </div>
          </div>

          <div className='flex gap-4 p-5 bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] rounded-2xl'>
            <div className='flex-shrink-0 w-10 h-10 rounded-xl bg-orange-500/10 border border-orange-500/20 flex items-center justify-center'>
              <Route className='w-5 h-5 text-orange-500' />
            </div>
            <div>
              <h2 className='text-[var(--color-text-hex)] font-bold mb-1'>Exploration Modes</h2>
              <p className='text-[var(--color-text-subtle-hex)] text-sm leading-relaxed'>
                There are two modes for exploration: Quadrant and Radius. In Quadrant mode, you
                start with a small circle and gradually explore in the cardinal directions. In
                Radius mode, you gradually expand outward.
              </p>
            </div>
          </div>
        </div>

        <div className='text-center space-y-4'>
          <p className='text-[var(--color-text-subtle-hex)] text-sm'>
            Ready to explore?
          </p>
          <div className='flex flex-col sm:flex-row gap-3 justify-center'>
            <Link
              to='/register'
              className='px-8 py-3 rounded-2xl bg-[var(--color-primary-hex)] text-white font-black text-sm uppercase tracking-widest hover:bg-[var(--color-primary-hover-hex)] transition-all shadow-xl shadow-orange-600/20 active:scale-[0.98]'
            >
              Register
            </Link>
            <Link
              to='/login'
              className='px-8 py-3 rounded-2xl bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] text-[var(--color-text-hex)] font-bold text-sm uppercase tracking-widest hover:border-orange-500/40 transition-all flex items-center justify-center gap-2'
            >
              Login
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
