import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { useAuth } from '../context/AuthContext';
import { Users as UsersIcon, Search, Key, ShieldCheck, Mail, Loader2, CheckCircle2, AlertCircle } from 'lucide-react';

interface User {
  id: string;
  username: string;
  email: string | null;
  name: string | null;
  weight: number;
}

export const UserManagement: React.FC = () => {
  const { token } = useAuth();
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [resettingId, setResettingId] = useState<string | null>(null);
  const [newPassword, setNewPassword] = useState('');
  const [message, setMessage] = useState<{ type: 'success' | 'error', text: string } | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createData, setCreateData] = useState({ username: '', password: '', name: '' });

  const fetchUsers = async () => {
    setLoading(true);
    try {
      const res = await axios.get('/api/admin/users', {
        headers: { Authorization: `Bearer ${token}` }
      });
      setUsers(res.data.items);
    } catch (err) {
      console.error('Failed to fetch users', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchUsers();
  }, [token]);

  const handleCreateUser = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await axios.post('/api/auth/register', createData);
      setMessage({ type: 'success', text: 'User created successfully' });
      setShowCreateModal(false);
      setCreateData({ username: '', password: '', name: '' });
      fetchUsers();
    } catch (err: any) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Failed to create user' });
    }
  };

  const handleResetPassword = async (userId: string) => {
    if (!newPassword) {
      setMessage({ type: 'error', text: 'Please enter a new password' });
      return;
    }

    try {
      await axios.post(`/api/admin/users/${userId}/reset-password`, 
        { newPassword },
        { headers: { Authorization: `Bearer ${token}` } }
      );
      setMessage({ type: 'success', text: 'Password reset successfully' });
      setResettingId(null);
      setNewPassword('');
      setTimeout(() => setMessage(null), 3000);
    } catch (err: any) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Failed to reset password' });
    }
  };

  const filteredUsers = users.filter(u => 
    (u.username?.toLowerCase().includes(search.toLowerCase()) ?? false) || 
    (u.name?.toLowerCase().includes(search.toLowerCase()) ?? false) || 
    (u.email?.toLowerCase().includes(search.toLowerCase()) ?? false)
  );

  return (
    <div className="flex flex-col h-full space-y-8 animate-in fade-in duration-700">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-6">
          <div className="w-16 h-16 bg-indigo-600 rounded-3xl flex items-center justify-center shadow-[0_0_30px_rgba(79,70,229,0.3)]">
            <UsersIcon size={32} className="text-white" />
          </div>
          <div>
            <h2 className="text-4xl font-black text-white tracking-tighter font-outfit uppercase">User Management</h2>
            <p className="text-zinc-500 font-bold tracking-[0.2em] text-[10px] uppercase mt-1">Manage system identities and access control</p>
          </div>
        </div>

        <div className="flex items-center gap-6">
          <div className="relative group w-80">
            <Search className="absolute left-6 top-1/2 -translate-y-1/2 text-zinc-600 group-focus-within:text-indigo-400 transition-colors" size={20} />
            <input
              type="text"
              placeholder="Search users..."
              className="w-full bg-[#09090b] border border-[#18181b] rounded-2xl pl-16 pr-6 py-4 text-zinc-300 focus:outline-none focus:ring-1 focus:ring-indigo-500/50 transition-all font-medium text-sm placeholder:text-zinc-800"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <button 
            onClick={() => setShowCreateModal(true)}
            className="h-14 bg-indigo-600 hover:bg-indigo-500 text-white px-8 rounded-2xl text-xs font-black flex items-center transition-all shadow-xl shadow-indigo-600/10 active:scale-95 uppercase tracking-widest"
          >
            Create User
          </button>
        </div>
      </div>

      {message && (
        <div className={`p-4 rounded-2xl flex items-center gap-4 animate-in slide-in-from-top-2 duration-300 border ${
          message.type === 'success' ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400' : 'bg-red-500/10 border-red-500/20 text-red-400'
        }`}>
          {message.type === 'success' ? <CheckCircle2 size={20} /> : <AlertCircle size={20} />}
          <span className="font-bold text-sm">{message.text}</span>
        </div>
      )}

      {/* Create User Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-8 animate-in fade-in duration-300">
          <div className="absolute inset-0 bg-zinc-950/80 backdrop-blur-xl" onClick={() => setShowCreateModal(false)} />
          <div className="bg-zinc-900 border border-zinc-800 w-full max-w-lg rounded-[48px] overflow-hidden shadow-[0_0_100px_rgba(0,0,0,1)] relative z-10 animate-in zoom-in-95 duration-500">
             <div className="p-10 border-b border-zinc-800 bg-zinc-950/40">
                <h3 className="font-black text-3xl text-white tracking-tighter font-outfit uppercase">Create Identity</h3>
                <p className="text-zinc-500 font-bold tracking-[0.2em] text-[10px] uppercase">Initialize a new system user</p>
             </div>
             <form onSubmit={handleCreateUser} className="p-10 space-y-6">
                <div className="space-y-2">
                   <label className="text-[10px] font-black text-zinc-600 uppercase tracking-widest ml-1">Username / Email</label>
                   <input
                      type="text"
                      className="w-full bg-zinc-950 border border-zinc-800 rounded-2xl px-6 py-4 text-white focus:ring-1 focus:ring-indigo-500 outline-none transition-all"
                      value={createData.username}
                      onChange={e => setCreateData({...createData, username: e.target.value})}
                      required
                   />
                </div>
                <div className="space-y-2">
                   <label className="text-[10px] font-black text-zinc-600 uppercase tracking-widest ml-1">Display Name</label>
                   <input
                      type="text"
                      className="w-full bg-zinc-950 border border-zinc-800 rounded-2xl px-6 py-4 text-white focus:ring-1 focus:ring-indigo-500 outline-none transition-all"
                      value={createData.name}
                      onChange={e => setCreateData({...createData, name: e.target.value})}
                   />
                </div>
                <div className="space-y-2">
                   <label className="text-[10px] font-black text-zinc-600 uppercase tracking-widest ml-1">Password</label>
                   <input
                      type="password"
                      className="w-full bg-zinc-950 border border-zinc-800 rounded-2xl px-6 py-4 text-white focus:ring-1 focus:ring-indigo-500 outline-none transition-all"
                      value={createData.password}
                      onChange={e => setCreateData({...createData, password: e.target.value})}
                      required
                   />
                </div>
                <div className="flex gap-4 mt-8">
                   <button type="submit" className="flex-1 h-14 bg-indigo-600 hover:bg-indigo-500 text-white rounded-2xl font-black uppercase tracking-widest transition-all">Create Account</button>
                   <button type="button" onClick={() => setShowCreateModal(false)} className="px-8 h-14 bg-zinc-800 hover:bg-zinc-700 text-zinc-400 rounded-2xl font-black uppercase tracking-widest transition-all">Cancel</button>
                </div>
             </form>
          </div>
        </div>
      )}

      <div className="bg-[#09090b] border border-[#18181b] rounded-[40px] overflow-hidden shadow-2xl">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-zinc-950/50 border-b border-[#18181b]">
                <th className="p-8 text-[10px] font-black text-zinc-600 uppercase tracking-[0.2em]">Profile</th>
                <th className="p-8 text-[10px] font-black text-zinc-600 uppercase tracking-[0.2em]">Email</th>
                <th className="p-8 text-[10px] font-black text-zinc-600 uppercase tracking-[0.2em]">Details</th>
                <th className="p-8 text-[10px] font-black text-zinc-600 uppercase tracking-[0.2em] text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#18181b]">
              {loading ? (
                <tr>
                  <td colSpan={4} className="p-20 text-center">
                    <div className="flex flex-col items-center gap-4">
                      <Loader2 className="w-8 h-8 text-indigo-500 animate-spin" />
                      <span className="text-zinc-600 font-bold uppercase tracking-widest text-xs">Synchronizing users...</span>
                    </div>
                  </td>
                </tr>
              ) : filteredUsers.length === 0 ? (
                <tr>
                  <td colSpan={4} className="p-20 text-center text-zinc-600 font-bold uppercase tracking-widest text-xs italic">No users found matching your search</td>
                </tr>
              ) : (
                filteredUsers.map((user) => (
                  <tr key={user.id} className="group hover:bg-zinc-900/30 transition-all">
                    <td className="p-8">
                      <div className="flex items-center gap-4">
                        <div className="w-12 h-12 rounded-2xl bg-zinc-900 border border-zinc-800 flex items-center justify-center text-zinc-600 group-hover:border-indigo-500/30 transition-all">
                          <ShieldCheck size={20} />
                        </div>
                        <div>
                          <p className="text-white font-black tracking-tight">{user.username || 'No Username'}</p>
                          <p className="text-[10px] text-zinc-600 font-mono uppercase tracking-tighter">{user.id}</p>
                        </div>
                      </div>
                    </td>
                    <td className="p-8">
                      <div className="flex items-center gap-3 text-zinc-400 font-medium">
                        <Mail size={16} className="text-zinc-700" />
                        {user.email || <span className="text-zinc-800 italic text-xs">No email provided</span>}
                      </div>
                    </td>
                    <td className="p-8">
                       <div className="space-y-1">
                          <p className="text-zinc-300 font-bold text-sm">{user.name || 'Anonymous User'}</p>
                          <p className="text-[10px] text-zinc-600 uppercase tracking-widest font-black">Weight: {user.weight}kg</p>
                       </div>
                    </td>
                    <td className="p-8">
                      <div className="flex justify-end items-center gap-3">
                        {resettingId === user.id ? (
                          <div className="flex items-center gap-3 animate-in slide-in-from-right-4 duration-300">
                            <input
                              type="password"
                              placeholder="New password"
                              className="bg-[#030304] border border-indigo-500/30 rounded-xl px-4 py-2 text-sm text-zinc-200 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                              value={newPassword}
                              onChange={(e) => setNewPassword(e.target.value)}
                              autoFocus
                            />
                            <button
                              onClick={() => handleResetPassword(user.id)}
                              className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white rounded-xl text-[10px] font-black uppercase tracking-widest transition-all shadow-lg shadow-indigo-600/20"
                            >
                              Reset
                            </button>
                            <button
                              onClick={() => { setResettingId(null); setNewPassword(''); }}
                              className="px-4 py-2 bg-zinc-800 hover:bg-zinc-700 text-zinc-400 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all"
                            >
                              Cancel
                            </button>
                          </div>
                        ) : (
                          <button
                            onClick={() => setResettingId(user.id)}
                            className="flex items-center gap-3 px-6 py-3 bg-zinc-900 hover:bg-zinc-800 border border-zinc-800 text-zinc-400 hover:text-indigo-400 rounded-2xl transition-all font-bold text-[10px] uppercase tracking-widest group/btn"
                          >
                            <Key size={14} className="group-hover/btn:rotate-12 transition-transform" />
                            Reset Password
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
