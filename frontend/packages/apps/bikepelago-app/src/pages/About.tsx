import type { ReactElement } from 'react';
import { Bike, Map, Trophy, Users, ArrowLeft, Route } from 'lucide-react';
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
          <p className='text-[var(--color-text-subtle-hex)] text-lg font-medium'>
            A multiplayer cycling adventure across the archipelago
          </p>
        </div>

        <div className='space-y-4 mb-10'>
          <div className='flex gap-4 p-5 bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] rounded-2xl'>
            <div className='flex-shrink-0 w-10 h-10 rounded-xl bg-orange-500/10 border border-orange-500/20 flex items-center justify-center'>
              <Map className='w-5 h-5 text-orange-500' />
            </div>
            <div>
              <h2 className='text-[var(--color-text-hex)] font-bold mb-1'>Explore the Archipelago</h2>
              <p className='text-[var(--color-text-subtle-hex)] text-sm leading-relaxed'>
                Bikeapelago is a cycling game built around Archipelago — a multiworld randomizer
                platform. Ride through stages to collect checks and unlock items for yourself and
                your fellow adventurers.
              </p>
            </div>
          </div>

          <div className='flex gap-4 p-5 bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] rounded-2xl'>
            <div className='flex-shrink-0 w-10 h-10 rounded-xl bg-orange-500/10 border border-orange-500/20 flex items-center justify-center'>
              <Route className='w-5 h-5 text-orange-500' />
            </div>
            <div>
              <h2 className='text-[var(--color-text-hex)] font-bold mb-1'>Ride to Progress</h2>
              <p className='text-[var(--color-text-subtle-hex)] text-sm leading-relaxed'>
                Each kilometre you ride in the real world translates to in-game progress. Connect
                your cycling activity, complete stages, and send checks to unlock the next leg of
                your journey.
              </p>
            </div>
          </div>

          <div className='flex gap-4 p-5 bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] rounded-2xl'>
            <div className='flex-shrink-0 w-10 h-10 rounded-xl bg-orange-500/10 border border-orange-500/20 flex items-center justify-center'>
              <Users className='w-5 h-5 text-orange-500' />
            </div>
            <div>
              <h2 className='text-[var(--color-text-hex)] font-bold mb-1'>Multiplayer Sessions</h2>
              <p className='text-[var(--color-text-subtle-hex)] text-sm leading-relaxed'>
                Create or join a game session with friends. Everyone's rides contribute to a shared
                adventure — keep pedalling to help the whole group reach the finish line together.
              </p>
            </div>
          </div>

          <div className='flex gap-4 p-5 bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] rounded-2xl'>
            <div className='flex-shrink-0 w-10 h-10 rounded-xl bg-orange-500/10 border border-orange-500/20 flex items-center justify-center'>
              <Trophy className='w-5 h-5 text-orange-500' />
            </div>
            <div>
              <h2 className='text-[var(--color-text-hex)] font-bold mb-1'>Track Your Achievements</h2>
              <p className='text-[var(--color-text-subtle-hex)] text-sm leading-relaxed'>
                Your athlete profile tracks every kilometre ridden and every check completed. Watch
                your progress accumulate as you conquer stage after stage across the islands.
              </p>
            </div>
          </div>
        </div>

        <div className='text-center space-y-4'>
          <p className='text-[var(--color-text-subtle-hex)] text-sm'>
            Ready to start riding?
          </p>
          <div className='flex flex-col sm:flex-row gap-3 justify-center'>
            <Link
              to='/register'
              className='px-8 py-3 rounded-2xl bg-[var(--color-primary-hex)] text-white font-black text-sm uppercase tracking-widest hover:bg-[var(--color-primary-hover-hex)] transition-all shadow-xl shadow-orange-600/20 active:scale-[0.98]'
            >
              Self-Register
            </Link>
            <Link
              to='/login'
              className='px-8 py-3 rounded-2xl bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] text-[var(--color-text-hex)] font-bold text-sm uppercase tracking-widest hover:border-orange-500/40 transition-all flex items-center justify-center gap-2'
            >
              <ArrowLeft className='w-4 h-4' />
              Back to Login
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
