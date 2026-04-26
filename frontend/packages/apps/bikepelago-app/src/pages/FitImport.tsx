import { useState } from 'react';

import {
  ArrowLeft,
  ArrowRight,
  CheckCircle2,
  FileCheck,
  Loader2,
  Monitor,
  User,
  XCircle,
} from 'lucide-react';
import { Navigate, useNavigate } from 'react-router-dom';

import { useFitAnalyzer } from '../hooks/useFitAnalyzer';
import { useSessions } from '../hooks/useSessions';
import { useAuthStore } from '../store/authStore';
import { useFitImportStore } from '../store/fitImportStore';
import type { FitAnalysisResult, GameSession } from '../types/game';

export default function FitImport() {
  const navigate = useNavigate();
  const { isValid } = useAuthStore();

  // Read the file reactively so it survives a login redirect (we don't clear it
  // until the user confirms or dismisses the import).
  const file = useFitImportStore((s) => s.pendingFile);
  const setPendingFile = useFitImportStore((s) => s.setPendingFile);

  const [selectedSession, setSelectedSession] = useState<GameSession | null>(
    null,
  );
  const [analysisResult, setAnalysisResult] =
    useState<FitAnalysisResult | null>(null);

  // Reset flow state whenever a new file arrives mid-session (adjust-state-during-render pattern)
  const [prevFile, setPrevFile] = useState(file);
  if (prevFile !== file) {
    setPrevFile(file);
    setSelectedSession(null);
    setAnalysisResult(null);
  }

  const { sessions, loading: sessionsLoading } = useSessions();
  const { analyzeFile, confirmValidation, loading, error, setError } =
    useFitAnalyzer(selectedSession?.id ?? '', setAnalysisResult);

  // Auth guard — file stays in store so it's still there after the user logs in.
  if (!isValid) {
    return <Navigate to='/login' state={{ returnTo: '/fit-import' }} />;
  }

  if (!file) {
    return <Navigate to='/' />;
  }

  const dismiss = () => {
    setPendingFile(null);
    void navigate('/');
  };

  const handleConfirm = async () => {
    const ok = await confirmValidation(analysisResult);
    if (ok) {
      setPendingFile(null);
      void navigate(`/game/${selectedSession!.id}`);
    }
  };

  // ── Phase 3: analysis results ─────────────────────────────────────────────
  if (analysisResult) {
    return (
      <div className='min-h-screen p-6 max-w-lg mx-auto flex flex-col'>
        <header className='flex items-center gap-3 mb-8'>
          <button
            onClick={() => setAnalysisResult(null)}
            className='p-2 rounded-xl bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] text-[var(--color-text-muted-hex)] hover:text-[var(--color-text-hex)] transition-colors'
          >
            <ArrowLeft className='w-5 h-5' />
          </button>
          <div>
            <h1 className='text-xl font-black text-[var(--color-text-hex)]'>
              Ride Analysis
            </h1>
            <p className='text-xs text-[var(--color-text-subtle-hex)]'>
              {selectedSession?.name ??
                selectedSession?.ap_seed_name ??
                'Session'}
            </p>
          </div>
        </header>

        <div className='grid grid-cols-2 gap-4 mb-6'>
          <div className='bg-[var(--color-surface-alt-hex)] p-4 rounded-xl border border-[var(--color-border-hex)]'>
            <span className='text-[10px] font-bold text-[var(--color-text-subtle-hex)] uppercase tracking-widest'>
              Distance
            </span>
            <div className='text-lg font-black mt-1 text-[var(--color-text-hex)]'>
              {(analysisResult.stats.distanceMeters / 1000).toFixed(2)}
              <span className='text-xs font-normal text-[var(--color-text-subtle-hex)] ml-1'>
                km
              </span>
            </div>
          </div>
          <div className='bg-[var(--color-surface-alt-hex)] p-4 rounded-xl border border-[var(--color-border-hex)]'>
            <span className='text-[10px] font-bold text-[var(--color-text-subtle-hex)] uppercase tracking-widest'>
              Duration
            </span>
            <div className='text-lg font-black mt-1 text-[var(--color-text-hex)]'>
              {Math.floor(analysisResult.stats.durationSeconds / 60)}
              <span className='text-xs font-normal text-[var(--color-text-subtle-hex)] ml-1'>
                min
              </span>
            </div>
          </div>
        </div>

        <h3 className='text-[10px] font-bold text-[var(--color-text-subtle-hex)] uppercase tracking-widest mb-3'>
          Locations Reached
        </h3>

        <div className='flex-1 overflow-y-auto mb-6 space-y-2'>
          {analysisResult.newlyCheckedNodes.length > 0 ? (
            analysisResult.newlyCheckedNodes.map((node) => (
              <div
                key={node.id}
                className='flex items-center gap-3 bg-emerald-500/10 p-4 rounded-xl border border-emerald-500/20 text-emerald-500'
              >
                <CheckCircle2 className='w-5 h-5 shrink-0' />
                <div className='flex flex-col'>
                  <span className='font-bold text-sm'>
                    Location{' '}
                    {node.apArrivalLocationId ?? node.ap_arrival_location_id}
                  </span>
                  <span className='text-[10px] opacity-70'>
                    [{node.lat.toFixed(5)}, {node.lon.toFixed(5)}]
                  </span>
                </div>
              </div>
            ))
          ) : (
            <div className='text-center py-10 text-[var(--color-text-subtle-hex)] text-sm'>
              No new locations reached in this ride.
            </div>
          )}
        </div>

        {error && (
          <div className='mb-4 p-3 bg-red-500/10 border border-red-500/20 rounded-xl text-red-500 text-xs flex items-center gap-2'>
            <XCircle className='w-4 h-4 shrink-0' />
            {error}
          </div>
        )}

        <div className='grid grid-cols-2 gap-4'>
          <button
            onClick={dismiss}
            className='p-4 rounded-xl font-bold border border-[var(--color-border-hex)] bg-[var(--color-surface-alt-hex)] text-[var(--color-text-hex)] hover:border-[var(--color-border-hex)]/60 transition-all'
          >
            Cancel
          </button>
          <button
            onClick={() => void handleConfirm()}
            disabled={analysisResult.newlyCheckedNodes.length === 0 || loading}
            className='p-4 rounded-xl font-bold bg-emerald-600 hover:bg-emerald-500 disabled:opacity-40 disabled:hover:bg-emerald-600 text-white transition-all flex items-center justify-center gap-2'
          >
            {loading ? (
              <Loader2 className='w-5 h-5 animate-spin' />
            ) : (
              'Confirm & Submit'
            )}
          </button>
        </div>
      </div>
    );
  }

  // ── Phase 2: session chosen — ready to analyze ────────────────────────────
  if (selectedSession) {
    return (
      <div className='min-h-screen p-6 max-w-lg mx-auto flex flex-col'>
        <header className='flex items-center gap-3 mb-8'>
          <button
            onClick={() => {
              setSelectedSession(null);
              setError(null);
            }}
            className='p-2 rounded-xl bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] text-[var(--color-text-muted-hex)] hover:text-[var(--color-text-hex)] transition-colors'
          >
            <ArrowLeft className='w-5 h-5' />
          </button>
          <h1 className='text-xl font-black text-[var(--color-text-hex)]'>
            Import Ride
          </h1>
        </header>

        <div className='flex-1 flex flex-col items-center justify-center gap-6'>
          <div className='p-4 bg-[var(--color-surface-alt-hex)] rounded-xl border border-[var(--color-border-hex)] flex items-center gap-3 w-full'>
            <FileCheck className='w-6 h-6 text-orange-500 shrink-0' />
            <span className='text-sm font-medium text-[var(--color-text-hex)] truncate'>
              {file.name}
            </span>
          </div>

          <div className='p-4 bg-[var(--color-surface-alt-hex)] rounded-xl border border-[var(--color-border-hex)] w-full'>
            <p className='text-[10px] font-bold text-[var(--color-text-subtle-hex)] uppercase tracking-widest mb-1'>
              Session
            </p>
            <p className='font-bold text-[var(--color-text-hex)]'>
              {selectedSession.name ??
                selectedSession.ap_seed_name ??
                'Unnamed Session'}
            </p>
          </div>

          {error && (
            <div className='p-3 bg-red-500/10 border border-red-500/20 rounded-xl text-red-500 text-xs flex items-center gap-2 w-full'>
              <XCircle className='w-4 h-4 shrink-0' />
              {error}
            </div>
          )}

          <button
            onClick={() => void analyzeFile(file)}
            disabled={loading}
            className='w-full h-14 rounded-2xl bg-orange-600 hover:bg-orange-500 disabled:opacity-50 text-white font-black text-base uppercase tracking-widest flex items-center justify-center gap-3 transition-all active:scale-[0.98]'
          >
            {loading ? (
              <>
                <Loader2 className='w-5 h-5 animate-spin' />
                Analyzing…
              </>
            ) : (
              <>
                Analyze Ride
                <ArrowRight className='w-5 h-5' />
              </>
            )}
          </button>
        </div>
      </div>
    );
  }

  // ── Phase 1: session picker ───────────────────────────────────────────────
  return (
    <div className='min-h-screen p-6 max-w-lg mx-auto flex flex-col'>
      <header className='flex items-center gap-3 mb-2'>
        <button
          onClick={dismiss}
          className='p-2 rounded-xl bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] text-[var(--color-text-muted-hex)] hover:text-[var(--color-text-hex)] transition-colors'
        >
          <ArrowLeft className='w-5 h-5' />
        </button>
        <h1 className='text-xl font-black text-[var(--color-text-hex)]'>
          Import Ride
        </h1>
      </header>

      <div className='flex items-center gap-2 mb-6 pl-1'>
        <FileCheck className='w-4 h-4 text-orange-500 shrink-0' />
        <span className='text-xs text-[var(--color-text-muted-hex)] truncate'>
          {file.name}
        </span>
      </div>

      <p className='text-sm text-[var(--color-text-muted-hex)] mb-6'>
        Choose which session to import this ride into.
      </p>

      {sessionsLoading && (
        <div className='flex items-center justify-center py-20 text-[var(--color-text-muted-hex)]'>
          <Loader2 className='w-6 h-6 animate-spin mr-2' />
          Loading sessions…
        </div>
      )}

      <div className='flex-1 overflow-y-auto space-y-3'>
        {sessions?.map((session) => (
          <button
            key={session.id}
            onClick={() => setSelectedSession(session)}
            className='group w-full text-left bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] rounded-2xl p-5 hover:border-orange-500/50 hover:shadow-lg hover:shadow-orange-500/10 transition-all duration-300 flex items-start justify-between gap-4'
          >
            <div className='flex-1 min-w-0'>
              <div className='flex items-center gap-2 mb-2'>
                <span
                  className={`px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-tight ${session.ap_server_url ? 'bg-orange-600 text-white' : 'bg-[var(--color-surface-hex)] text-[var(--color-text-muted-hex)]'}`}
                >
                  {session.ap_server_url ? 'Archipelago' : 'Single Player'}
                </span>
              </div>
              <p className='font-bold text-[var(--color-text-hex)] truncate group-hover:text-orange-500 transition-colors'>
                {session.name ?? session.ap_seed_name ?? 'Unnamed Session'}
              </p>
              <div className='mt-1.5 space-y-0.5'>
                {session.ap_server_url && (
                  <div className='flex items-center gap-1.5 text-xs text-[var(--color-text-subtle-hex)]'>
                    <Monitor className='w-3 h-3' />
                    <span className='truncate'>{session.ap_server_url}</span>
                  </div>
                )}
                <div className='flex items-center gap-1.5 text-xs text-[var(--color-text-subtle-hex)]'>
                  <User className='w-3 h-3' />
                  <span>{session.ap_slot_name}</span>
                </div>
              </div>
            </div>
            <ArrowRight className='w-5 h-5 text-[var(--color-text-subtle-hex)] group-hover:text-orange-500 shrink-0 mt-1 transition-colors' />
          </button>
        ))}

        {!sessionsLoading && sessions?.length === 0 && (
          <div className='text-center py-16 text-[var(--color-text-subtle-hex)] text-sm'>
            No active sessions found.
          </div>
        )}
      </div>
    </div>
  );
}
