import React, { useState } from 'react';

import axios from 'axios';
import {
  Activity,
  ChevronRight,
  Lock,
  ShieldCheck,
  Terminal,
  User as UserIcon,
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';

import { useAuth } from '../context/AuthContext';

export const Login: React.FC = () => {
  const [identity, setIdentity] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const res = await axios.post('/api/auth/login', { identity, password });
      const { token, record } = res.data;

      login(token, record);
      navigate('/');
    } catch (err: any) {
      setError(
        err.response?.data?.message ?? 'Gateway timeout or unauthorized link.',
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className='h-screen w-screen bg-[#020203] flex items-center justify-center relative overflow-hidden font-sans select-none antialiased'>
      {/* Dynamic Background Elements */}
      <div className='absolute top-0 left-0 w-full h-full'>
        <div className='absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[1200px] h-[1200px] bg-indigo-500/5 blur-[180px] rounded-full animate-pulse' />
        <div className='absolute bottom-0 right-1/4 w-[600px] h-[600px] bg-emerald-500/5 blur-[140px] rounded-full' />
        <div className='absolute top-0 left-1/4 w-[600px] h-[600px] bg-rose-500/5 blur-[140px] rounded-full' />
      </div>

      <div className='w-full max-w-lg p-1 relative z-10'>
        <div className='bg-[#09090b] border border-[#18181b] rounded-[48px] p-10 shadow-2xl relative overflow-hidden'>
          {/* Subtle line background */}
          <div className='absolute inset-x-0 bottom-0 h-px bg-gradient-to-r from-transparent via-indigo-500/20 to-transparent' />
          <div className='absolute inset-y-0 right-0 w-px bg-gradient-to-b from-transparent via-indigo-500/10 to-transparent' />

          <div className='flex flex-col items-center mb-12 animate-up'>
            <div className='flex items-center gap-3 px-6 py-2 bg-indigo-500/5 border border-indigo-500/10 rounded-full mb-8 backdrop-blur-xl'>
              <ShieldCheck size={16} className='text-indigo-400' />
              <span className='text-[10px] font-bold text-indigo-400 uppercase tracking-[0.2em]'>
                Authentication Required
              </span>
            </div>

            <div className='flex items-center justify-center -space-x-3 mb-6'>
              <div className='w-16 h-16 bg-[#18181b] border border-[#27272a] rounded-3xl rotate-12 flex items-center justify-center shadow-xl'>
                <Terminal size={32} strokeWidth={1} className='text-zinc-600' />
              </div>
              <div className='w-20 h-20 bg-indigo-600 rounded-[32px] flex items-center justify-center shadow-[0_0_40px_rgba(79,70,229,0.4)] relative z-10 -rotate-3 border-4 border-[#09090b]'>
                <Activity
                  size={40}
                  strokeWidth={2.5}
                  className='text-white drop-shadow-lg'
                />
              </div>
            </div>
          </div>

          <form onSubmit={handleSubmit} className='space-y-6'>
            <div className='space-y-2 group'>
              <label className='text-[10px] font-black text-zinc-600 uppercase tracking-[0.25em] ml-1 group-focus-within:text-indigo-400 transition-colors'>
                Username or Email
              </label>
              <div className='relative'>
                <UserIcon
                  className='absolute left-6 top-1/2 -translate-y-1/2 text-zinc-600 group-focus-within:text-indigo-500 transition-all'
                  size={20}
                />
                <input
                  type='text'
                  placeholder='admin@bikeapelago.com'
                  className='w-full bg-[#030304] border border-[#18181b] rounded-3xl pl-16 pr-6 py-5 text-zinc-100 focus:outline-none focus:ring-1 focus:ring-indigo-500/50 transition-all font-medium text-lg placeholder:text-zinc-800'
                  value={identity}
                  onChange={(e) => setIdentity(e.target.value)}
                  required
                />
              </div>
            </div>

            <div className='space-y-2 group'>
              <label className='text-[10px] font-black text-zinc-600 uppercase tracking-[0.25em] ml-1 group-focus-within:text-indigo-400 transition-colors'>
                Password
              </label>
              <div className='relative'>
                <Lock
                  className='absolute left-6 top-1/2 -translate-y-1/2 text-zinc-600 group-focus-within:text-indigo-500 transition-all'
                  size={20}
                />
                <input
                  type='password'
                  placeholder='••••••••••••'
                  className='w-full bg-[#030304] border border-[#18181b] rounded-3xl pl-16 pr-6 py-5 text-zinc-100 focus:outline-none focus:ring-1 focus:ring-indigo-500/50 transition-all font-medium text-lg placeholder:text-zinc-800'
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                />
              </div>
            </div>

            {error && (
              <div className='p-5 bg-red-500/5 border border-red-500/10 rounded-3xl flex items-center text-red-400 text-sm animate-in slide-in-from-top-2 duration-300'>
                <div className='flex-1 font-medium'>{error}</div>
              </div>
            )}

            <button
              type='submit'
              disabled={loading}
              className='w-full h-20 bg-indigo-600 hover:bg-indigo-500 text-white font-black rounded-[28px] transition-all shadow-2xl shadow-indigo-600/10 active:scale-[0.98] flex items-center justify-center group disabled:opacity-50 mt-4 overflow-hidden relative'
            >
              <div className='absolute inset-0 bg-white/10 translate-y-full group-hover:translate-y-0 transition-transform duration-500' />
              {loading ? (
                <div className='w-6 h-6 border-3 border-white/20 border-t-white rounded-full animate-spin relative z-10' />
              ) : (
                <div className='flex items-center gap-3 relative z-10'>
                  <span className='text-xl tracking-tight'>Login</span>
                  <ChevronRight
                    size={24}
                    className='transform group-hover:translate-x-1.5 transition-transform duration-300'
                  />
                </div>
              )}
            </button>
          </form>

          <footer className='mt-12 flex justify-center items-center px-2 opacity-30'>
            <div className='text-[10px] font-bold text-zinc-500 uppercase tracking-[0.2em]'>
              Bikeapelago Admin Portal
            </div>
          </footer>
        </div>
      </div>
    </div>
  );
};
