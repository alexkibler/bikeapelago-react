import React, { useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import Sidebar from './Sidebar';
import Header from './Header';
import BottomNav from './BottomNav';
import ToastContainer from './ToastContainer';

interface LayoutProps {
  children: React.ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const location = useLocation();
  const navigate = useNavigate();
  const isGamePage = location.pathname.startsWith('/game');
  const { isValid } = useAuthStore();

  useEffect(() => {
    if (!isValid && location.pathname !== '/login') {
      navigate('/login');
    }
  }, [isValid, location.pathname, navigate]);

  return (
    <div className={`min-h-screen bg-[var(--color-surface-alt-hex)] flex flex-col md:flex-row ${isGamePage ? 'h-screen' : ''}`}>
      {/* Desktop Sidebar (hidden on mobile) */}
      <Sidebar />

      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        {/* Mobile Header */}
        <div className="bg-[var(--color-surface-alt-hex)] sticky top-0 shrink-0 md:hidden z-50">
          <Header />
        </div>

        {/* Main Content Area */}
        <main className={isGamePage ? 'flex-1 flex flex-col w-full min-h-0 relative' : 'flex-1 overflow-y-auto w-full pb-24 md:pb-8'}>
          {children}
        </main>
      </div>

      {/* Mobile Bottom Nav */}
      <BottomNav />

      {/* Global Notifications */}
      <ToastContainer />
    </div>
  );
};

export default Layout;
