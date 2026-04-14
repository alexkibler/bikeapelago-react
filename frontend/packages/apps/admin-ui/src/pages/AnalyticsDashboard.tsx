import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { useAuth } from '../context/AuthContext';
import {
  BarChart3,
  Users,
  Activity,
  Battery,
  MapPin,
  TrendingUp,
  ChevronRight,
  Zap,
  Target,
} from 'lucide-react';

export const AnalyticsDashboard: React.FC = () => {
  const { token } = useAuth();
  const [summary, setSummary] = useState<any>(null);
  const [traffic, setTraffic] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [sumRes, trafRes] = await Promise.all([
          axios.get('/api/admin/analytics/summary', {
            headers: { Authorization: `Bearer ${token}` },
          }),
          axios.get('/api/admin/analytics/traffic', {
            headers: { Authorization: `Bearer ${token}` },
          }),
        ]);
        setSummary(sumRes.data);
        setTraffic(trafRes.data);
      } catch (err) {
        console.error('Failed to fetch analytics', err);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [token]);

  if (loading)
    return (
      <div className='flex h-full items-center justify-center'>
        <div className='w-12 h-12 border-4 border-indigo-500/10 border-t-indigo-500 rounded-full animate-spin' />
      </div>
    );

  const stats = [
    {
      label: 'Total Users',
      value: summary?.totalUsers.toLocaleString(),
      change: '+12%',
      icon: <Users size={28} />,
      color: 'indigo',
    },
    {
      label: 'Active Sessions',
      value: summary?.activeSessions,
      change: '+5%',
      icon: <Activity size={28} />,
      color: 'emerald',
    },
    {
      label: 'API Utilization',
      value: `${summary?.apiUtilization}%`,
      change: '-2%',
      icon: <Zap size={28} />,
      color: 'amber',
    },
    {
      label: 'Nodes Discovered',
      value: summary?.totalNodes.toLocaleString(),
      change: '+842',
      icon: <MapPin size={28} />,
      color: 'rose',
    },
  ];

  return (
    <div className='space-y-10 animate-up'>
      <header className='flex flex-col md:flex-row md:items-end justify-between gap-6'>
        <div>
          <div className='flex items-center gap-3 mb-4'>
            <div className='w-1.5 h-6 bg-indigo-600 rounded-full' />
            <span className='text-[10px] font-black text-indigo-400 uppercase tracking-[0.4em]'>
              Operational
            </span>
          </div>
          <h1 className='text-6xl font-black text-white tracking-tighter font-outfit uppercase'>
            Analytics <span className='text-zinc-600'>Dashboard</span>
          </h1>
          <p className='text-zinc-500 font-bold text-sm mt-3 tracking-wide'>
            Overview of system performance and activity.
          </p>
        </div>
        <div className='flex gap-4'>
          <button className='px-8 py-4 bg-[#09090b] border border-[#18181b] rounded-2xl text-zinc-400 font-bold text-xs uppercase tracking-widest hover:text-white transition-colors'>
            Export Report
          </button>
          <button className='px-8 py-4 bg-indigo-600 hover:bg-indigo-500 text-white font-black rounded-2xl text-xs uppercase tracking-widest shadow-xl shadow-indigo-600/10'>
            Full Scan
          </button>
        </div>
      </header>

      {/* Stats Grid */}
      <div className='grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6'>
        {stats.map((stat, i) => (
          <div
            key={i}
            className='bg-[#09090b] border border-[#18181b] rounded-[36px] p-8 hover:border-zinc-700 transition-all group relative overflow-hidden'
          >
            {/* Background glow hover */}
            <div
              className={`absolute top-0 right-0 w-24 h-24 blur-3xl opacity-0 group-hover:opacity-20 transition-opacity bg-${stat.color}-500/50`}
            />

            <div className='flex items-center justify-between mb-8'>
              <div
                className={`w-14 h-14 bg-${stat.color}-500/10 border border-${stat.color}-500/20 rounded-2xl flex items-center justify-center text-${stat.color}-400 shadow-inner group-hover:scale-110 transition-transform`}
              >
                {stat.icon}
              </div>
              <div
                className={`text-xs font-bold ${stat.change.startsWith('+') ? 'text-emerald-500' : 'text-red-500'} flex items-center`}
              >
                <TrendingUp size={14} className='mr-1.5' />
                {stat.change}
              </div>
            </div>

            <h3 className='text-[10px] font-black text-zinc-600 uppercase tracking-[0.25em] mb-2'>
              {stat.label}
            </h3>
            <p className='text-4xl font-black text-white tracking-tighter font-outfit'>
              {stat.value}
            </p>
          </div>
        ))}
      </div>

      <div className='grid grid-cols-1 lg:grid-cols-3 gap-8'>
        {/* Main Chart Card */}
        <div className='lg:col-span-2 bg-[#09090b] border border-[#18181b] rounded-[48px] p-10 overflow-hidden relative'>
          <div className='flex items-center justify-between mb-12'>
            <div className='flex items-center gap-6'>
              <div className='w-14 h-14 rounded-3xl bg-indigo-600/5 border border-indigo-500/10 flex items-center justify-center text-indigo-500'>
                <BarChart3 size={24} />
              </div>
              <div>
                <h3 className='text-xl font-black text-zinc-100 tracking-tight font-outfit uppercase'>
                  Traffic Activity
                </h3>
                <p className='text-xs text-zinc-600 font-bold uppercase tracking-widest'>
                  Gateway Request Volume / 24h
                </p>
              </div>
            </div>
            <div className='flex items-center gap-2 px-4 py-2 bg-zinc-950 border border-zinc-800 rounded-2xl'>
              <div className='w-1.5 h-1.5 rounded-full bg-emerald-500' />
              <span className='text-[10px] font-bold text-zinc-400 uppercase tracking-widest'>
                Real-time
              </span>
            </div>
          </div>

          <div className='h-72 flex items-end justify-between gap-3 mb-4'>
            {(traffic?.data || [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]).map(
              (h: number, i: number) => {
                const max = Math.max(...(traffic?.data || [1]), 1);
                const height = (h / max) * 100;
                return (
                  <div
                    key={i}
                    className='flex-1 bg-indigo-600/20 border border-indigo-500/10 rounded-xl hover:bg-indigo-500 group relative transition-all'
                    style={{ height: `${Math.max(height, 5)}%` }}
                  >
                    <div className='absolute top-0 left-1/2 -translate-x-1/2 -translate-y-full mb-3 opacity-0 group-hover:opacity-100 transition-opacity bg-white text-zinc-950 text-[10px] font-black px-2 py-1 rounded-md z-30 whitespace-nowrap'>
                      {h} REQS / {traffic?.labels[i]}
                    </div>
                  </div>
                );
              },
            )}
          </div>
          <div className='flex justify-between px-2'>
            {traffic?.labels
              .filter((_: any, i: number) => i % 6 === 0)
              .map((l: string, i: number) => (
                <span
                  key={i}
                  className='text-[10px] font-black text-zinc-700 uppercase tracking-tighter'
                >
                  {l}
                </span>
              ))}
            <span className='text-[10px] font-black text-zinc-700 uppercase tracking-tighter'>
              {traffic?.labels[23]}
            </span>
          </div>
        </div>

        {/* Small Action Cards */}
        <div className='space-y-6'>
          <div className='bg-gradient-to-br from-indigo-600 to-indigo-900 rounded-[48px] p-10 text-white relative overflow-hidden group shadow-2xl shadow-indigo-600/20'>
            <div className='absolute top-0 right-0 w-32 h-32 bg-white/20 blur-3xl opacity-0 group-hover:opacity-100 transition-opacity' />
            <div className='relative z-10'>
              <Target size={40} strokeWidth={2.5} className='mb-8' />
              <h3 className='text-3xl font-black tracking-tighter font-outfit uppercase leading-tight mb-4'>
                API Rate Limiting
              </h3>
              <p className='text-white/60 text-sm font-bold mb-8'>
                Rate limiting is currently active for the Overpass API.
              </p>
              <button className='w-full h-14 bg-white text-indigo-600 rounded-2xl flex items-center justify-center gap-3 font-black text-xs uppercase tracking-widest shadow-xl active:scale-[0.98] transition-all'>
                Release Throttle
                <ChevronRight size={18} />
              </button>
            </div>
          </div>

          <div className='bg-[#09090b] border border-[#18181b] rounded-[48px] p-10 flex items-center gap-8 group hover:bg-zinc-900/50 transition-all'>
            <div className='w-20 h-20 bg-emerald-500/10 border border-emerald-500/20 rounded-[32px] flex items-center justify-center text-emerald-400 flex-shrink-0 group-hover:scale-110 transition-transform'>
              <Battery size={32} />
            </div>
            <div>
              <h4 className='text-xl font-black text-white tracking-tighter font-outfit uppercase'>
                Database Status
              </h4>
              <p className='text-xs text-zinc-600 font-bold uppercase tracking-widest'>
                Connection Optimal
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
