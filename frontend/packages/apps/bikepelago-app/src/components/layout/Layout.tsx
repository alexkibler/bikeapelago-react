import React, { useEffect } from 'react';

import { useLocation, useNavigate } from 'react-router-dom';

import { useAuthStore } from '../../store/authStore';
import BottomNav from './BottomNav';
import Header from './Header';
import Sidebar from './Sidebar';
import ToastContainer from './ToastContainer';

interface LayoutProps {
  children: React.ReactNode;
}

const Layout = ({ children }: LayoutProps) => {
  const location = useLocation();
  const navigate = useNavigate();
  const isGamePage = location.pathname.startsWith('/game');
  const { isValid } = useAuthStore();
  const isPublicPage =
    location.pathname === '/login' ||
    location.pathname === '/register' ||
    location.pathname === '/about';

  useEffect(() => {
    if (!isValid && !isPublicPage) {
      void navigate('/login');
    }
  }, [isPublicPage, isValid, navigate]);

  const isAuthPage = isPublicPage;

  return (
    <div
      className={`min-h-screen bg-[var(--color-surface-alt-hex)] flex flex-col md:flex-row ${isGamePage ? 'h-screen' : ''}`}
    >
      {/* Desktop Sidebar (hidden on mobile and auth pages) */}
      {!isAuthPage && <Sidebar />}

      <div className='flex-1 flex flex-col min-w-0 overflow-hidden'>
        {/* Mobile Header */}
        {!isAuthPage && (
          <div className='bg-[var(--color-surface-alt-hex)] sticky top-0 shrink-0 md:hidden z-50 pt-[env(safe-area-inset-top)]'>
            <Header />
          </div>
        )}

        {/* Main Content Area */}
        <main
          className={
            isGamePage
              ? 'flex-1 flex flex-col w-full min-h-0 relative'
              : `flex-1 overflow-y-auto w-full ${!isAuthPage ? 'pb-24 md:pb-8' : ''}`
          }
        >
          {children}
        </main>
      </div>

      {/* Mobile Bottom Nav */}
      {!isAuthPage && <BottomNav />}

      {/* Global Notifications */}
      <ToastContainer />
    </div>
  );
};

export default Layout;
