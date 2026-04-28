import React from 'react';

import { Navigate, useLocation } from 'react-router-dom';

import { useAuth } from '../../context/AuthContext';

export const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const { user, loading } = useAuth();
  const location = useLocation();

  if (loading) {
    return (
      <div className='h-screen w-screen bg-zinc-950 flex flex-col items-center justify-center space-y-4'>
        <div className='w-12 h-12 border-4 border-indigo-500/20 border-t-indigo-500 rounded-full animate-spin shadow-2xl shadow-indigo-500/20' />
        <p className='text-zinc-500 text-sm font-bold tracking-widest uppercase animate-pulse'>
          Initializing Security...
        </p>
      </div>
    );
  }

  if (!user) {
    return <Navigate to='/login' state={{ from: location }} replace />;
  }

  return <>{children}</>;
};
