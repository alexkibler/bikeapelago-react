import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Globe, User, Lock, Server, ArrowRight, Info } from 'lucide-react';

const NewGame = () => {
  const navigate = useNavigate();
  const [gameMode, setGameMode] = useState<'archipelago' | 'singleplayer'>('archipelago');
  const [serverUrl, setServerUrl] = useState('archipelago.gg:');
  const [slotName, setSlotName] = useState('');
  const [password, setPassword] = useState('');
  const [errorMsg, setErrorMsg] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (gameMode === 'archipelago' && !slotName) {
      setErrorMsg('Slot name is required for Archipelago connection.');
      return;
    }

    // Direct redirect without creating a DB session placeholder yet
    const params = new URLSearchParams();
    params.set('mode', gameMode);
    if (gameMode === 'archipelago') {
      params.set('serverUrl', serverUrl);
      params.set('slotName', slotName);
    }
    
    navigate(`/setup-session?${params.toString()}`);
  };


  return (
    <div className="max-w-2xl mx-auto py-12 px-6">
      <div className="bg-neutral-900 border border-neutral-800 rounded-3xl p-8 md:p-12 shadow-2xl relative overflow-hidden">
        {/* Glow effect */}
        <div className="absolute -top-24 -right-24 w-48 h-48 bg-orange-600/20 blur-[80px] rounded-full"></div>
        
        <header className="relative mb-10">
          <h1 className="text-white text-3xl font-black mb-2 tracking-tight">Create New Game</h1>
          <p className="text-neutral-500">Configure your session parameters to get started.</p>
        </header>

        <form onSubmit={handleSubmit} className="space-y-8 relative">
          {/* Game Mode Selection */}
          <div className="grid grid-cols-2 gap-4">
            <button
              type="button"
              onClick={() => setGameMode('archipelago')}
              className={`p-4 rounded-2xl border-2 transition-all flex flex-col items-center gap-3 ${
                gameMode === 'archipelago' 
                ? 'border-orange-500 bg-orange-500/5 text-white' 
                : 'border-neutral-800 bg-neutral-800/50 text-neutral-500 hover:border-neutral-700'
              }`}
            >
              <Globe className={`w-8 h-8 ${gameMode === 'archipelago' ? 'text-orange-500' : ''}`} />
              <span className="font-bold text-sm">Archipelago</span>
            </button>
            <button
              type="button"
              onClick={() => setGameMode('singleplayer')}
              className={`p-4 rounded-2xl border-2 transition-all flex flex-col items-center gap-3 ${
                gameMode === 'singleplayer' 
                ? 'border-orange-500 bg-orange-500/5 text-white' 
                : 'border-neutral-800 bg-neutral-800/50 text-neutral-500 hover:border-neutral-700'
              }`}
            >
              <User className={`w-8 h-8 ${gameMode === 'singleplayer' ? 'text-orange-500' : ''}`} />
              <span className="font-bold text-sm">Single Player</span>
            </button>
          </div>

          {gameMode === 'archipelago' && (
            <div className="space-y-6 animate-in fade-in slide-in-from-top-4 duration-500">
              <div className="space-y-2">
                <label className="text-xs font-black uppercase tracking-widest text-neutral-500 ml-1">Server URL</label>
                <div className="relative group">
                  <Server className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-neutral-600 group-focus-within:text-orange-500 transition-colors" />
                  <input
                    type="text"
                    value={serverUrl}
                    onChange={(e) => setServerUrl(e.target.value)}
                    placeholder="archipelago.gg:12345"
                    className="w-full bg-neutral-800 border border-neutral-700 rounded-xl py-4 pl-12 pr-4 text-white focus:outline-none focus:ring-2 focus:ring-orange-500/50 focus:border-orange-500 transition-all"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <label className="text-xs font-black uppercase tracking-widest text-neutral-500 ml-1">Slot Name</label>
                <div className="relative group">
                  <User className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-neutral-600 group-focus-within:text-orange-500 transition-colors" />
                  <input
                    type="text"
                    value={slotName}
                    onChange={(e) => setSlotName(e.target.value)}
                    placeholder="Enter your slot name"
                    className="w-full bg-neutral-800 border border-neutral-700 rounded-xl py-4 pl-12 pr-4 text-white focus:outline-none focus:ring-2 focus:ring-orange-500/50 focus:border-orange-500 transition-all"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <label className="text-xs font-black uppercase tracking-widest text-neutral-500 ml-1">Password (Optional)</label>
                <div className="relative group">
                  <Lock className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-neutral-600 group-focus-within:text-orange-500 transition-colors" />
                  <input
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    placeholder="••••••••"
                    className="w-full bg-neutral-800 border border-neutral-700 rounded-xl py-4 pl-12 pr-4 text-white focus:outline-none focus:ring-2 focus:ring-orange-500/50 focus:border-orange-500 transition-all"
                  />
                </div>
              </div>
            </div>
          )}

          {gameMode === 'singleplayer' && (
            <div className="p-6 rounded-2xl bg-orange-500/5 border border-orange-500/20 flex gap-4 animate-in fade-in slide-in-from-top-4 duration-500">
              <Info className="w-6 h-6 text-orange-500 shrink-0" />
              <p className="text-sm text-neutral-400 leading-relaxed">
                In Single Player mode, you'll still be able to use the map and routing tools, but your progress won't be synced with a multiworld server.
              </p>
            </div>
          )}

          <div className="pt-4">
            <button
              type="submit"
              className="w-full btn btn-orange btn-lg h-16 rounded-2xl font-black uppercase tracking-widest text-sm flex items-center justify-center gap-3 disabled:opacity-50 disabled:bg-neutral-800 group shadow-xl shadow-orange-600/10"
            >
              {gameMode === 'archipelago' ? 'Connect & Play' : 'Start Single Player'}
              <ArrowRight className="w-5 h-5 group-hover:translate-x-1 transition-transform" />
            </button>
          </div>

          {errorMsg && (
            <div className="p-4 rounded-xl bg-red-500/10 border border-red-500/20 text-red-400 text-sm font-medium animate-in shake duration-300">
              {errorMsg}
            </div>
          )}
        </form>
      </div>
    </div>
  );
};

export default NewGame;
