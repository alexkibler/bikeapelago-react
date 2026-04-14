import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  PlayCircle,
  User,
  ArrowRight,
  Plus,
  Monitor,
  Loader2,
  Download,
  Trash2,
  AlertTriangle,
} from 'lucide-react';
import { useSessions } from '../hooks/useSessions';
import type { GameSession } from '../types/game';
import { useAuthStore } from '../store/authStore';
import { toast } from '../store/toastStore';

const DeleteSessionDialog = ({
  session,
  onConfirm,
  onCancel,
  isDeleting,
}: {
  session: GameSession;
  onConfirm: () => void;
  onCancel: () => void;
  isDeleting: boolean;
}) => {
  return (
    <div
      className='fixed inset-0 z-[2000] flex items-center justify-center p-4'
      role='dialog'
      aria-modal='true'
    >
      {/* Backdrop */}
      <div
        className='absolute inset-0 bg-black/60 backdrop-blur-md transition-opacity duration-300'
        onClick={!isDeleting ? onCancel : undefined}
      />

      {/* Panel */}
      <div className='relative w-full max-w-sm bg-[var(--color-surface-hex)] border border-[var(--color-border-hex)] rounded-2xl shadow-2xl overflow-hidden animate-in fade-in zoom-in duration-200'>
        <div className='h-1.5 w-full bg-red-600' />
        <div className='p-8'>
          <div className='flex items-center justify-center w-14 h-14 rounded-full bg-red-500/10 mb-6 mx-auto'>
            <AlertTriangle className='w-7 h-7 text-red-500' />
          </div>

          <h2 className='text-xl font-bold text-[var(--color-text-hex)] text-center mb-2'>
            Delete Session?
          </h2>
          <p className='text-[var(--color-text-muted-hex)] text-center text-sm mb-8 leading-relaxed'>
            Are you sure you want to delete{' '}
            <span className='text-[var(--color-text-hex)] font-semibold'>
              "{session.name || session.ap_seed_name || 'Unnamed Session'}"
            </span>
            ? This action is permanent and will delete all nodes and routes
            associated with it.
          </p>

          <div className='flex gap-3'>
            <button
              onClick={onCancel}
              disabled={isDeleting}
              className='flex-1 px-4 py-3 text-xs font-black uppercase tracking-widest rounded-xl border border-[var(--color-border-hex)] text-[var(--color-text-muted-hex)] hover:bg-[var(--color-surface-alt-hex)] hover:text-[var(--color-text-hex)] transition-all disabled:opacity-50'
            >
              Cancel
            </button>
            <button
              onClick={onConfirm}
              disabled={isDeleting}
              className='flex-1 px-4 py-3 text-xs font-black uppercase tracking-widest rounded-xl bg-red-600 hover:bg-red-500 text-white shadow-lg shadow-red-900/20 transition-all active:scale-95 disabled:opacity-50 flex items-center justify-center gap-2'
            >
              {isDeleting ? (
                <>
                  <Loader2 className='w-4 h-4 animate-spin' />
                  Deleting...
                </>
              ) : (
                'Confirm Delete'
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

const Home = () => {
  const { sessions, loading, error, deleteSession } = useSessions();
  const { user } = useAuthStore();
  const [sessionToDelete, setSessionToDelete] = useState<GameSession | null>(
    null,
  );

  const isAp = (session: GameSession) => !!session.ap_server_url;

  const handleDelete = () => {
    if (!sessionToDelete) return;

    deleteSession.mutate(
      {
        id: sessionToDelete.id,
      },
      {
        onSuccess: () => {
          setSessionToDelete(null);
          toast.success('Session deleted successfully.');
        },
        onError: () => {
          toast.error('Failed to delete session. Please try again.');
        },
      },
    );
  };

  return (
    <div className='py-12 px-6 max-w-screen-xl mx-auto'>
      {/* Header Section */}
      <header className='mb-12 text-center max-w-2xl mx-auto'>
        <h1 className='text-[var(--color-text-hex)] text-4xl md:text-5xl font-black mb-4 tracking-tight text-glow'>
          Welcome Back, {user?.name || user?.username || 'Rider'}
        </h1>
        <div className='inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-orange-500/10 border border-orange-500/20 text-orange-500 text-xs font-bold uppercase tracking-[0.2em] mb-6'>
          <span className='w-1.5 h-1.5 bg-orange-500 rounded-full animate-pulse'></span>
          Active Game Sessions
        </div>
      </header>

      {/* Delete Confirmation Modal */}
      {sessionToDelete && (
        <DeleteSessionDialog
          session={sessionToDelete}
          onConfirm={handleDelete}
          onCancel={() => setSessionToDelete(null)}
          isDeleting={deleteSession.isPending}
        />
      )}

      {/* Loading / Error states */}
      {loading && (
        <div className='flex items-center justify-center py-20 text-[var(--color-text-muted-hex)]'>
          <Loader2 className='w-8 h-8 animate-spin mr-3' />
          Loading sessions…
        </div>
      )}
      {!loading && error && (
        <p className='text-center text-red-400 py-10'>{error}</p>
      )}

      {/* Sessions Grid */}
      {!loading && !error && (
        <div className='grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6'>
          {sessions?.map((session) => (
            <div
              key={session.id}
              className='group relative overflow-hidden bg-[var(--color-surface-hex)] border border-[var(--color-border-hex)] rounded-2xl p-6 hover:border-orange-500/50 transition-all duration-500 hover:shadow-2xl hover:shadow-orange-500/10 flex flex-col min-h-[260px]'
            >
              <div className='absolute inset-0 bg-gradient-to-br from-orange-500/5 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-500'></div>

              {/* Trash Can Button */}
              <button
                onClick={(e) => {
                  e.preventDefault();
                  e.stopPropagation();
                  setSessionToDelete(session);
                }}
                className='absolute top-4 right-4 p-2 rounded-lg bg-neutral-800/50 text-neutral-500 hover:bg-red-500/20 hover:text-red-500 transition-all opacity-0 group-hover:opacity-100 z-10'
                title='Delete Session'
              >
                <Trash2 className='w-4 h-4' />
              </button>

              <div className='relative flex-grow flex flex-col'>
                <div className='flex items-center justify-between mb-3'>
                  <div
                    className={`px-2.5 py-1 rounded-md text-[10px] font-black uppercase tracking-tighter ${isAp(session) ? 'bg-orange-600 text-white' : 'bg-[var(--color-surface-alt-hex)] text-[var(--color-text-muted-hex)]'}`}
                  >
                    {isAp(session) ? 'Archipelago' : 'Single Player'}
                  </div>
                  <div className='text-[var(--color-text-muted-hex)] group-hover:text-orange-500 transition-colors mr-8'>
                    <PlayCircle className='w-6 h-6' />
                  </div>
                </div>

                <h3 className='text-xl text-[var(--color-text-hex)] font-bold mb-3 group-hover:text-orange-500 transition-colors truncate pr-8'>
                  {session.name || session.ap_seed_name || 'Unnamed Session'}
                </h3>

                {isAp(session) ? (
                  <div className='space-y-1.5 mb-4'>
                    <div className='flex items-center gap-2 text-sm text-[var(--color-text-muted-hex)]'>
                      <Monitor className='w-4 h-4' />
                      <span className='truncate'>{session.ap_server_url}</span>
                    </div>
                    <div className='flex items-center gap-2 text-sm text-[var(--color-text-muted-hex)]'>
                      <User className='w-4 h-4' />
                      <span>{session.ap_slot_name}</span>
                    </div>
                  </div>
                ) : (
                  <div className='space-y-1.5 mb-4'>
                    <div className='flex items-center gap-2 text-sm text-[var(--color-text-muted-hex)]'>
                      <Monitor className='w-4 h-4' />
                      <span>Local Discovery</span>
                    </div>
                    <div className='flex items-center gap-2 text-sm text-[var(--color-text-muted-hex)]'>
                      <User className='w-4 h-4' />
                      <span>
                        {user?.name || user?.username || 'Local Rider'}
                      </span>
                    </div>
                  </div>
                )}

                <Link
                  to={`/game/${session.id}`}
                  className='w-full btn btn-orange btn-md flex items-center justify-center gap-2 group/btn mt-auto'
                >
                  Resume Session
                  <ArrowRight className='w-4 h-4 group-hover/btn:translate-x-1 transition-transform' />
                </Link>
              </div>
            </div>
          ))}

          {sessions?.length === 0 && (
            <p className='col-span-full text-center text-neutral-500 py-10'>
              No sessions yet. Start a new one!
            </p>
          )}

          {/* New Game Card */}
          <Link
            to='/new-game'
            className='group relative overflow-hidden bg-[var(--color-surface-hex)] border-2 border-dashed border-[var(--color-border-hex)] rounded-2xl p-6 flex flex-col items-center justify-center text-center hover:border-orange-500/50 transition-all duration-500 min-h-[260px]'
          >
            <div className='w-12 h-12 bg-[var(--color-surface-alt-hex)] rounded-full flex items-center justify-center mb-4 group-hover:bg-orange-500/10 group-hover:scale-110 transition-all duration-500'>
              <Plus className='w-6 h-6 text-[var(--color-text-muted-hex)] group-hover:text-orange-500' />
            </div>
            <h3 className='text-xl text-[var(--color-text-hex)] font-bold mb-2'>
              New Session
            </h3>
            <p className='text-[var(--color-text-muted-hex)] text-sm max-w-[200px]'>
              Connect to a new Archipelago server or start a local world.
            </p>
          </Link>

          {/* APWorld Download Card */}
          <a
            href='/bikeapelago.apworld'
            download='bikeapelago.apworld'
            className='group relative overflow-hidden bg-[var(--color-surface-hex)] border border-[var(--color-border-hex)] rounded-2xl p-6 flex flex-col items-center justify-center text-center hover:border-orange-500/50 transition-all duration-500 min-h-[260px]'
          >
            <div className='w-12 h-12 bg-[var(--color-surface-alt-hex)] rounded-full flex items-center justify-center mb-4 group-hover:bg-orange-500/10 group-hover:scale-110 transition-all duration-500'>
              <Download className='w-6 h-6 text-[var(--color-text-muted-hex)] group-hover:text-orange-500' />
            </div>
            <h3 className='text-xl text-[var(--color-text-hex)] font-bold mb-2'>
              APWorld Plugin
            </h3>
            <p className='text-[var(--color-text-muted-hex)] text-sm max-w-[200px]'>
              Download the Archipelago plugin to generate your own seed.
            </p>
          </a>
        </div>
      )}
    </div>
  );
};

export default Home;
