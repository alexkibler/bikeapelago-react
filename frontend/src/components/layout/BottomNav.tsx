import React from 'react';
import { useLocation, Link } from 'react-router-dom';
import { Home, MessageSquare, UploadCloud, Map } from 'lucide-react';
import { useGameStore } from '../../store/gameStore';

const BottomNav = () => {
  const location = useLocation();
  const isGamePage = location.pathname.startsWith('/game');
  const pathname = location.pathname;
  const { activePanel, togglePanel } = useGameStore();

  return (
    <div className="fixed bottom-0 left-0 right-0 z-[2000] border-t border-[var(--color-border-hex)] bg-[var(--color-surface-hex)] px-0 pb-safe pt-0 md:hidden">
      <div className="mx-auto flex max-w-lg items-center justify-around px-6 pt-2 pb-2">
        {isGamePage ? (
          <>
            <Link to="/" className="flex flex-col items-center gap-1 p-2 text-[var(--color-text-muted-hex)] transition-colors hover:text-[var(--color-primary-hex)]">
              <Home className="w-5 h-5" />
              <span className="text-[10px] font-bold uppercase tracking-wider">Home</span>
            </Link>
            <button
              onClick={() => togglePanel('chat')}
              className={`flex flex-col items-center gap-1 p-2 transition-colors rounded-lg ${activePanel === 'chat' ? 'bg-[var(--color-primary-hex)]/20 text-[var(--color-primary-hex)]' : 'text-[var(--color-text-muted-hex)] hover:text-[var(--color-primary-hex)] hover:bg-[rgb(var(--color-surface-overlay))]'}`}>
              <MessageSquare className="w-5 h-5" />
              <span className="text-[10px] font-bold uppercase tracking-wider">Chat</span>
            </button>
            <button
              onClick={() => togglePanel('upload')}
              className={`flex flex-col items-center gap-1 p-2 transition-colors rounded-lg ${activePanel === 'upload' ? 'bg-[var(--color-primary-hex)]/20 text-[var(--color-primary-hex)]' : 'text-[var(--color-text-muted-hex)] hover:text-[var(--color-primary-hex)] hover:bg-[rgb(var(--color-surface-overlay))]'}`}>
              <UploadCloud className="w-5 h-5" />
              <span className="text-[10px] font-bold uppercase tracking-wider">Upload .fit</span>
            </button>
            <button
              onClick={() => togglePanel('route')}
              className={`flex flex-col items-center gap-1 p-2 transition-colors rounded-lg ${activePanel === 'route' ? 'bg-[var(--color-primary-hex)]/20 text-[var(--color-primary-hex)]' : 'text-[var(--color-text-muted-hex)] hover:text-[var(--color-primary-hex)] hover:bg-[rgb(var(--color-surface-overlay))]'}`}>
              <Map className="w-5 h-5" />
              <span className="text-[10px] font-bold uppercase tracking-wider">Route Builder</span>
            </button>
          </>
        ) : (
          <>
            <Link to="/" className={`flex flex-col items-center gap-1 p-2 transition-colors ${pathname === '/' ? 'text-[var(--color-primary-hex)]' : 'text-[var(--color-text-muted-hex)] hover:text-[var(--color-primary-hex)]'}`}>
              <Home className="w-5 h-5" />
              <span className="text-[10px] font-bold uppercase tracking-wider">Home</span>
            </Link>
            <Link to="/new-game" className={`flex flex-col items-center gap-1 p-2 transition-colors ${pathname.startsWith('/new-game') ? 'text-[var(--color-primary-hex)]' : 'text-[var(--color-text-muted-hex)] hover:text-[var(--color-primary-hex)]'}`}>
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M6 11V6a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v5" /><path d="M4 19a2 2 0 0 1-2-2v-2a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v2a2 2 0 0 1-2 2H4Z" /><path d="M6 15v-2" /><path d="M10 15v-2" /><path d="M14 15v-2" /><path d="M18 15v-2" /></svg>
              <span className="text-[10px] font-bold uppercase tracking-wider">Play</span>
            </Link>
            <Link to="/yaml-creator" className={`flex flex-col items-center gap-1 p-2 transition-colors ${pathname === '/yaml-creator' ? 'text-[var(--color-primary-hex)]' : 'text-[var(--color-text-muted-hex)] hover:text-[var(--color-primary-hex)]'}`}>
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" /><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" /></svg>
              <span className="text-[10px] font-bold uppercase tracking-wider">Create</span>
            </Link>
          </>
        )}
      </div>
    </div>
  );
};

export default BottomNav;
