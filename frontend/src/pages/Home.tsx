import React from 'react';
import { Link } from 'react-router-dom';
import { PlayCircle, User, ArrowRight, Plus, Monitor, Loader2, Download } from 'lucide-react';
import { useSessions, type GameSession } from '../hooks/useSessions';
import { useAuthStore } from '../store/authStore';

const Home = () => {
  const { sessions, loading, error } = useSessions();
  const { user } = useAuthStore();

  const isAp = (session: GameSession) => !!session.ap_server_url;

  return (
    <div className="py-12 px-6 max-w-screen-xl mx-auto">
      {/* Header Section */}
      <header className="mb-12 text-center max-w-2xl mx-auto">
        <h1 className="text-white text-4xl md:text-5xl font-black mb-4 tracking-tight">
          Welcome Back, {user?.name || user?.username || 'Rider'}
        </h1>
        <div className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-orange-500/10 border border-orange-500/20 text-orange-500 text-xs font-bold uppercase tracking-[0.2em] mb-6">
          <span className="w-1.5 h-1.5 bg-orange-500 rounded-full animate-pulse"></span>
          Active Game Sessions
        </div>
      </header>

      {/* Loading / Error states */}
      {loading && (
        <div className="flex items-center justify-center py-20 text-neutral-400">
          <Loader2 className="w-8 h-8 animate-spin mr-3" />
          Loading sessions…
        </div>
      )}
      {!loading && error && (
        <p className="text-center text-red-400 py-10">{error}</p>
      )}

      {/* Sessions Grid */}
      {!loading && !error && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {sessions.map((session) => (
            <div
              key={session.id}
              className="group relative overflow-hidden bg-neutral-900 border border-neutral-800 rounded-2xl p-6 hover:border-orange-500/50 transition-all duration-500 hover:shadow-2xl hover:shadow-orange-500/10 flex flex-col min-h-[260px]"
            >
              <div className="absolute inset-0 bg-gradient-to-br from-orange-500/5 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-500"></div>

              <div className="relative flex-grow flex flex-col">
                <div className="flex items-center justify-between mb-3">
                  <div className={`px-2.5 py-1 rounded-md text-[10px] font-black uppercase tracking-tighter ${isAp(session) ? 'bg-orange-600 text-white' : 'bg-neutral-800 text-neutral-400'}`}>
                    {isAp(session) ? 'Archipelago' : 'Single Player'}
                  </div>
                  <div className="text-neutral-500 group-hover:text-orange-500 transition-colors">
                    <PlayCircle className="w-6 h-6" />
                  </div>
                </div>

                <h3 className="text-xl text-white font-bold mb-3 group-hover:text-orange-500 transition-colors truncate">
                  {session.name || session.ap_seed_name || 'Unnamed Session'}
                </h3>

                {isAp(session) ? (
                  <div className="space-y-1.5 mb-4">
                    <div className="flex items-center gap-2 text-sm text-neutral-400">
                      <Monitor className="w-4 h-4" />
                      <span className="truncate">{session.ap_server_url}</span>
                    </div>
                    <div className="flex items-center gap-2 text-sm text-neutral-400">
                      <User className="w-4 h-4" />
                      <span>{session.ap_slot_name}</span>
                    </div>
                  </div>
                ) : (
                  <div className="space-y-1.5 mb-4">
                    <div className="flex items-center gap-2 text-sm text-neutral-400">
                      <Monitor className="w-4 h-4" />
                      <span>Local Discovery</span>
                    </div>
                    <div className="flex items-center gap-2 text-sm text-neutral-400">
                      <User className="w-4 h-4" />
                      <span>{user?.name || user?.username || 'Local Rider'}</span>
                    </div>
                  </div>
                )}

                <Link
                  to={`/game/${session.id}`}
                  className="w-full btn btn-orange btn-md flex items-center justify-center gap-2 group/btn mt-auto"
                >
                  Resume Session
                  <ArrowRight className="w-4 h-4 group-hover/btn:translate-x-1 transition-transform" />
                </Link>
              </div>
            </div>
          ))}

          {sessions.length === 0 && (
            <p className="col-span-full text-center text-neutral-500 py-10">No sessions yet. Start a new one!</p>
          )}

          {/* New Game Card */}
          <Link
            to="/new-game"
            className="group relative overflow-hidden bg-neutral-900 border-2 border-dashed border-neutral-800 rounded-2xl p-6 flex flex-col items-center justify-center text-center hover:border-orange-500/50 transition-all duration-500 min-h-[260px]"
          >
            <div className="w-12 h-12 bg-neutral-800 rounded-full flex items-center justify-center mb-4 group-hover:bg-orange-500/10 group-hover:scale-110 transition-all duration-500">
              <Plus className="w-6 h-6 text-neutral-500 group-hover:text-orange-500" />
            </div>
            <h3 className="text-xl text-white font-bold mb-2">New Session</h3>
            <p className="text-neutral-500 text-sm max-w-[200px]">
              Connect to a new Archipelago server or start a local world.
            </p>
          </Link>

          {/* APWorld Download Card */}
          <a
            href="/bikeapelago.apworld"
            download="bikeapelago.apworld"
            className="group relative overflow-hidden bg-neutral-900 border border-neutral-800 rounded-2xl p-6 flex flex-col items-center justify-center text-center hover:border-orange-500/50 transition-all duration-500 min-h-[260px]"
          >
            <div className="w-12 h-12 bg-neutral-800 rounded-full flex items-center justify-center mb-4 group-hover:bg-orange-500/10 group-hover:scale-110 transition-all duration-500">
              <Download className="w-6 h-6 text-neutral-500 group-hover:text-orange-500" />
            </div>
            <h3 className="text-xl text-white font-bold mb-2">APWorld Plugin</h3>
            <p className="text-neutral-500 text-sm max-w-[200px]">
              Download the Archipelago plugin to generate your own seed.
            </p>
          </a>
        </div>
      )}
    </div>
  );
};

export default Home;
