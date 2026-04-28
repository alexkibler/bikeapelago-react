import React, { useState } from 'react';

import {
  Activity,
  BarChart3,
  Cpu,
  Database,
  LogOut,
  Menu,
  Shield,
  Users,
  X,
} from 'lucide-react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';

import { useAuth } from '../../context/AuthContext';

export const DashboardLayout: React.FC = () => {
  const { logout, user } = useAuth();
  const navigate = useNavigate();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const handleLogout = () => {
    logout();
    void navigate('/login');
  };

  const navItems = [
    { to: '/analytics', icon: <BarChart3 size={20} />, label: 'Analytics' },
    { to: '/users', icon: <Users size={20} />, label: 'Users' },
    { to: '/explorer', icon: <Database size={20} />, label: 'Data Explorer' },
  ];

  return (
    <div className='flex h-screen bg-[#020203] text-zinc-400 font-sans antialiased overflow-hidden'>
      {/* Dynamic Background */}
      <div className='fixed inset-0 pointer-events-none'>
        <div className='absolute top-0 right-0 w-[500px] h-[500px] bg-indigo-500/5 blur-[120px] rounded-full' />
        <div className='absolute bottom-0 left-0 w-[500px] h-[500px] bg-emerald-500/5 blur-[120px] rounded-full' />
      </div>

      {/* Sidebar */}
      <aside
        className={`
        fixed inset-y-0 left-0 z-50 w-72 bg-[#09090b] border-r border-[#18181b] 
        transform transition-transform duration-500 ease-in-out lg:relative lg:translate-x-0
        ${isMobileMenuOpen ? 'translate-x-0' : '-translate-x-full'}
      `}
      >
        <div className='flex flex-col h-full p-6'>
          <div className='flex items-center gap-4 mb-12 px-2'>
            <div className='w-12 h-12 bg-indigo-600 rounded-2xl flex items-center justify-center shadow-[0_0_20px_rgba(79,70,229,0.3)]'>
              <Activity size={24} className='text-white' />
            </div>
            <div>
              <h1 className='text-xl font-black text-white tracking-tighter font-outfit uppercase'>
                Admin<span className='text-indigo-500'>Panel</span>
              </h1>
              <div className='flex items-center gap-1.5 opacity-50'>
                <div className='w-1 h-1 rounded-full bg-emerald-500' />
                <span className='text-[10px] font-bold tracking-widest uppercase'>
                  Connected
                </span>
              </div>
            </div>
          </div>

          <nav className='flex-1 space-y-2'>
            <div className='text-[10px] font-black text-zinc-600 uppercase tracking-[0.2em] mb-4 ml-4'>
              Management
            </div>
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) => `
                  flex items-center gap-4 px-4 py-4 rounded-2xl transition-all group
                  ${
                    isActive
                      ? 'bg-indigo-500/10 text-indigo-400 border border-indigo-500/20 shadow-lg shadow-indigo-500/5'
                      : 'hover:bg-zinc-900 text-zinc-500 hover:text-zinc-200'
                  }
                `}
              >
                <span className='p-1.5 rounded-lg bg-zinc-950 border border-zinc-800 group-hover:border-zinc-700 transition-colors'>
                  {item.icon}
                </span>
                <span className='font-bold text-sm tracking-tight'>
                  {item.label}
                </span>
              </NavLink>
            ))}
          </nav>

          <div className='pt-6 border-t border-[#18181b] mt-6'>
            <div className='bg-zinc-950/50 rounded-3xl p-4 border border-zinc-800/50 mb-6'>
              <div className='flex items-center gap-3 mb-3'>
                <div className='w-8 h-8 rounded-full bg-zinc-800 flex items-center justify-center border border-zinc-700 overflow-hidden'>
                  {user?.avatar ? (
                    <img
                      src={user.avatar}
                      className='w-full h-full object-cover'
                    />
                  ) : (
                    <Shield size={16} className='text-zinc-500' />
                  )}
                </div>
                <div className='flex-1 overflow-hidden'>
                  <p className='text-xs font-bold text-white truncate'>
                    {user?.name || 'Administrator'}
                  </p>
                  <p className='text-[10px] text-zinc-600 font-mono tracking-tighter truncate uppercase'>
                    {user?.userName}
                  </p>
                </div>
              </div>
              <div className='flex gap-2'>
                <div className='flex-1 h-1 bg-zinc-900 rounded-full overflow-hidden'>
                  <div className='h-full bg-emerald-500 w-full' />
                </div>
              </div>
            </div>

            <button
              onClick={handleLogout}
              className='flex items-center gap-4 w-full px-6 py-4 rounded-2xl text-zinc-500 hover:text-red-400 hover:bg-red-500/5 transition-all group'
            >
              <LogOut
                size={20}
                className='group-hover:rotate-12 transition-transform'
              />
              <span className='font-bold text-sm tracking-tight'>Logout</span>
            </button>
          </div>
        </div>
      </aside>

      {/* Main Content */}
      <main className='flex-1 flex flex-col min-w-0 relative z-10'>
        <header className='h-20 border-b border-[#18181b] flex items-center justify-between px-8 bg-[#09090b]/50 backdrop-blur-md'>
          <div className='flex items-center gap-6'>
            <button
              onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
              className='lg:hidden p-2 text-zinc-500'
            >
              {isMobileMenuOpen ? <X size={24} /> : <Menu size={24} />}
            </button>
            <div className='flex items-center gap-3 text-zinc-600'>
              <Cpu size={16} />
              <span className='text-[10px] font-bold tracking-[0.3em] uppercase'>
                Control Panel
              </span>
            </div>
          </div>

          <div className='flex items-center gap-4'>
            <div className='px-4 py-1.5 bg-emerald-500/5 border border-emerald-500/10 rounded-full flex items-center gap-2'>
              <div className='w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse' />
              <span className='text-[10px] font-bold text-emerald-500 uppercase tracking-widest'>
                Database Linked
              </span>
            </div>
          </div>
        </header>

        <div className='flex-1 overflow-y-auto custom-scrollbar p-10'>
          <Outlet />
        </div>
      </main>
    </div>
  );
};
