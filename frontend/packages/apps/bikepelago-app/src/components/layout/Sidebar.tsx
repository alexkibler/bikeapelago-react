import React from 'react';
import { useLocation, Link, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import { useGameStore, type GamePanel } from '../../store/gameStore';
import ThemeToggle from './ThemeToggle';
import { Package } from 'lucide-react';

const Sidebar = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuthStore();
  const { activePanel, setActivePanel } = useGameStore();

  const pathname = location.pathname;
  const isGamePage = pathname.startsWith('/game');
  const session = useGameStore((s) => s.session);
  const canInteract = !!session?.ap_server_url && session?.status !== 'Completed';

  const togglePanel = (panel: GamePanel) => {
    setActivePanel(activePanel === panel ? null : panel);
  };

  const handleLogout = (e: React.FormEvent) => {
    e.preventDefault();
    logout();
    void navigate('/login');
  };

  return (
    <aside className="hidden md:flex flex-col w-64 h-screen bg-[var(--color-surface-hex)] border-r border-[var(--color-border-hex)] sticky top-0 shrink-0 z-[1001]">
      {/* Logo Section */}
      <div className="p-6 flex items-center justify-between">
        <Link to="/" className="flex items-center">
          <span className="text-[var(--color-primary-hex)] uppercase font-extrabold text-2xl tracking-tighter italic">bikeapelago</span>
        </Link>
        <div className="flex items-center">
          <ThemeToggle />
        </div>
      </div>

      {/* Navigation Links */}
      <nav className="flex-1 px-4 space-y-2 mt-4">
        {isGamePage ? (
          <>
            <Link to="/" className="flex items-center gap-3 px-4 py-3 rounded-xl transition-all duration-200 text-[var(--color-text-muted-hex)] hover:text-[var(--color-text-hex)] hover:bg-[rgb(var(--color-surface-overlay))]">
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m3 9 9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
              <span className="font-medium text-sm">Return Home</span>
            </Link>

            <div className="h-px bg-[var(--color-border-hex)] my-4"></div>

            <button
              onClick={() => togglePanel('chat')}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-xl transition-all duration-200 ${activePanel === 'chat' ? 'bg-[var(--color-primary-hex)]/10 text-[var(--color-primary-hex)] shadow-inner shadow-primary/5' : 'text-[var(--color-text-muted-hex)] hover:text-[var(--color-text-hex)] hover:bg-[rgb(var(--color-surface-overlay))]'}`}>
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>
              <span className="font-medium text-sm">Game Chat</span>
            </button>

            {canInteract && (
              <button
                onClick={() => togglePanel('inventory')}
                className={`w-full flex items-center gap-3 px-4 py-3 rounded-xl transition-all duration-200 ${activePanel === 'inventory' ? 'bg-[var(--color-primary-hex)]/10 text-[var(--color-primary-hex)] shadow-inner shadow-primary/5' : 'text-[var(--color-text-muted-hex)] hover:text-[var(--color-text-hex)] hover:bg-[rgb(var(--color-surface-overlay))]'}`}>
                <Package className="w-5 h-5" />
                <span className="font-medium text-sm">Inventory</span>
              </button>
            )}

            {canInteract && (
              <button
                onClick={() => togglePanel('route')}
                className={`w-full flex items-center gap-3 px-4 py-3 rounded-xl transition-all duration-200 ${activePanel === 'route' ? 'bg-[var(--color-primary-hex)]/10 text-[var(--color-primary-hex)] shadow-inner shadow-primary/5' : 'text-[var(--color-text-muted-hex)] hover:text-[var(--color-text-hex)] hover:bg-[rgb(var(--color-surface-overlay))]'}`}>
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m3 6 6-3 6 3 6-3v15l-6 3-6-3-6 3Z"/><path d="M9 3v15"/><path d="M15 6v15"/></svg>
                <span className="font-medium text-sm">Route Builder</span>
              </button>
            )}
          </>
        ) : (
          <>
            <Link to="/" className={`flex items-center gap-3 px-4 py-3 rounded-xl transition-all duration-200 ${pathname === '/' ? 'bg-[var(--color-primary-hex)]/10 text-[var(--color-primary-hex)] shadow-inner shadow-primary/5' : 'text-[var(--color-text-muted-hex)] hover:text-[var(--color-text-hex)] hover:bg-[rgb(var(--color-surface-overlay))]'}`}>
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m3 9 9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
              <span className="font-medium text-sm">Dashboard</span>
            </Link>

            <Link to="/new-game" className={`flex items-center gap-3 px-4 py-3 rounded-xl transition-all duration-200 ${pathname.startsWith('/new-game') || pathname.startsWith('/setup-session') ? 'bg-[var(--color-primary-hex)]/10 text-[var(--color-primary-hex)] shadow-inner shadow-primary/5' : 'text-[var(--color-text-muted-hex)] hover:text-[var(--color-text-hex)] hover:bg-[rgb(var(--color-surface-overlay))]'}`}>
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M6 11V6a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v5"/><path d="M4 19a2 2 0 0 1-2-2v-2a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v2a2 2 0 0 1-2 2H4Z"/><path d="M6 15v-2"/><path d="M10 15v-2"/><path d="M14 15v-2"/><path d="M18 15v-2"/></svg>
              <span className="font-medium text-sm">Play Now</span>
            </Link>

            <Link to="/yaml-creator" className={`flex items-center gap-3 px-4 py-3 rounded-xl transition-all duration-200 ${pathname === '/yaml-creator' ? 'bg-[var(--color-primary-hex)]/10 text-[var(--color-primary-hex)] shadow-inner shadow-primary/5' : 'text-[var(--color-text-muted-hex)] hover:text-[var(--color-text-hex)] hover:bg-[rgb(var(--color-surface-overlay))]'}`}>
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
              <span className="font-medium text-sm">Create YAML</span>
            </Link>

            <Link to="/settings" className={`flex items-center gap-3 px-4 py-3 rounded-xl transition-all duration-200 ${pathname === '/settings' ? 'bg-[var(--color-primary-hex)]/10 text-[var(--color-primary-hex)] shadow-inner shadow-primary/5' : 'text-[var(--color-text-muted-hex)] hover:text-[var(--color-text-hex)] hover:bg-[rgb(var(--color-surface-overlay))]'}`}>
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z"/><circle cx="12" cy="12" r="3"/></svg>
              <span className="font-medium text-sm">Settings</span>
            </Link>
          </>
        )}
      </nav>

      {/* Logout */}
      {user && (
        <div className="p-4 mt-auto border-t border-[var(--color-border-hex)]">
          <button onClick={handleLogout} className="w-full flex items-center justify-center gap-3 p-3 rounded-xl hover:bg-[var(--color-error-hex)]/10 hover:text-[var(--color-error-hex)] text-[var(--color-text-muted-hex)] transition-all duration-200 group">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" x2="9" y1="12" y2="12"/></svg>
            <span className="font-bold text-xs uppercase tracking-widest">Sign Out</span>
          </button>
        </div>
      )}
    </aside>
  );
};

export default Sidebar;
