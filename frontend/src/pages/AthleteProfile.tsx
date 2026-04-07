import React, { useState, useEffect } from 'react';
import { User, Camera, Save, LogOut, ChevronRight, Edit3 } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore, getToken } from '../store/authStore';

const AthleteProfile = () => {
  const navigate = useNavigate();
  const { user: authUser, logout } = useAuthStore();

  const [name, setName] = useState('');
  const [weight, setWeight] = useState(75.0);
  const [isEditing, setIsEditing] = useState(false);
  const [tempName, setTempName] = useState('');
  const [tempWeight, setTempWeight] = useState(75.0);
  const [saving, setSaving] = useState(false);

  // Populate from auth state
  useEffect(() => {
    if (authUser) {
      const n = authUser.name || authUser.username || '';
      const w = authUser.weight ?? 75.0;
      setName(n);
      setWeight(w);
      setTempName(n);
      setTempWeight(w);
    }
  }, [authUser]);

  const handleEdit = () => {
    setTempName(name);
    setTempWeight(weight);
    setIsEditing(true);
  };

  const handleSave = async () => {
    if (!authUser?.id) return;
    setSaving(true);
    try {
      const token = getToken();
      const res = await fetch(`/api/users/${authUser.id}`, {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        },
        body: JSON.stringify({ name: tempName, weight: tempWeight }),
      });
      if (!res.ok) throw new Error(`Failed to save profile: ${res.status}`);
      setName(tempName);
      setWeight(tempWeight);
    } catch (err) {
      console.error('Failed to save profile', err);
    } finally {
      setSaving(false);
      setIsEditing(false);
    }
  };

  const handleSignOut = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="max-w-2xl mx-auto py-12 px-6">
      <header className="flex items-center justify-between mb-12 border-b border-neutral-800 pb-6">
        <h1 className="text-3xl font-black text-white italic uppercase tracking-tighter">My Profile</h1>
        {isEditing ? (
          <button 
            onClick={handleSave}
            disabled={saving}
            className="btn btn-orange btn-sm rounded-full px-6 gap-2"
          >
            <Save className="w-4 h-4" />
            {saving ? 'Saving...' : 'Save Changes'}
          </button>
        ) : (
          <button 
            onClick={handleEdit}
            className="btn btn-neutral btn-sm rounded-full px-6 gap-2 border-neutral-800"
          >
            <Edit3 className="w-4 h-4" />
            Edit Profile
          </button>
        )}
      </header>

      <div className="space-y-8">
        {/* Avatar Section */}
        <div className="flex flex-col items-center py-8 bg-neutral-900 border border-neutral-800 rounded-3xl relative overflow-hidden group">
          <div className="absolute inset-0 bg-gradient-to-b from-orange-500/5 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-500"></div>
          
          <div className="relative">
            <div className="w-32 h-32 rounded-full bg-neutral-800 border-4 border-neutral-800 overflow-hidden shadow-2xl relative group/avatar">
              <User className="w-full h-full p-6 text-neutral-600" />
              <label className="absolute inset-0 bg-black/40 flex items-center justify-center opacity-0 group-hover/avatar:opacity-100 transition-opacity cursor-pointer">
                <Camera className="w-8 h-8 text-white" />
                <input type="file" className="hidden" accept="image/*" />
              </label>
            </div>
            <div className="absolute -bottom-1 -right-1 w-10 h-10 bg-orange-600 rounded-full border-4 border-neutral-950 flex items-center justify-center shadow-lg">
              <Camera className="w-4 h-4 text-white" />
            </div>
          </div>

          <div className="mt-6 text-center">
            <h2 className="text-xl font-black text-white">{name}</h2>
            <p className="text-sm text-neutral-500 italic">@{authUser?.username}</p>
          </div>
        </div>

        {/* Stats Section */}
        <div className="grid grid-cols-1 gap-4">
          <div className="bg-neutral-900 border border-neutral-800 rounded-2xl p-6 flex items-center justify-between group hover:border-orange-500/30 transition-colors">
            <div className="flex items-center gap-4">
              <div className="p-3 bg-neutral-800 rounded-xl text-neutral-500 group-hover:text-orange-500 transition-colors">
                <User className="w-5 h-5" />
              </div>
              <div>
                <span className="text-[10px] font-black uppercase tracking-widest text-neutral-500">Full Name</span>
                {isEditing ? (
                  <input 
                    type="text" 
                    value={tempName} 
                    onChange={(e) => setTempName(e.target.value)}
                    className="block bg-neutral-800 border-b border-orange-500 text-white font-bold py-1 focus:outline-none"
                  />
                ) : (
                  <p className="text-white font-bold">{name}</p>
                )}
              </div>
            </div>
            {!isEditing && <ChevronRight className="w-4 h-4 text-neutral-700" />}
          </div>

          <div className="bg-neutral-900 border border-neutral-800 rounded-2xl p-6 flex items-center justify-between group hover:border-orange-500/30 transition-colors">
            <div className="flex items-center gap-4">
              <div className="p-3 bg-neutral-800 rounded-xl text-neutral-500 group-hover:text-orange-500 transition-colors">
                <span className="text-sm font-black">kg</span>
              </div>
              <div>
                <span className="text-[10px] font-black uppercase tracking-widest text-neutral-500">Weight</span>
                {isEditing ? (
                  <div className="flex items-center gap-2">
                    <input 
                      type="number" 
                      value={tempWeight} 
                      onChange={(e) => setTempWeight(Number(e.target.value))}
                      className="block bg-neutral-800 border-b border-orange-500 text-white font-bold py-1 w-20 focus:outline-none"
                    />
                    <span className="text-neutral-500 font-bold">kg</span>
                  </div>
                ) : (
                  <p className="text-white font-bold">{weight} <span className="text-neutral-500">kg</span></p>
                )}
              </div>
            </div>
            {!isEditing && <ChevronRight className="w-4 h-4 text-neutral-700" />}
          </div>
        </div>

        {/* Danger Zone */}
        <div className="pt-8 flex justify-center">
          <button
            onClick={handleSignOut}
            className="flex items-center gap-2 px-8 py-3 bg-neutral-900 border border-neutral-800 rounded-full text-neutral-500 hover:text-red-500 hover:bg-red-500/5 hover:border-red-500/20 transition-all font-bold text-sm uppercase tracking-widest group"
          >
            <LogOut className="w-4 h-4 group-hover:-translate-x-1 transition-transform" />
            Sign Out
          </button>
        </div>
      </div>
    </div>
  );
};

export default AthleteProfile;

