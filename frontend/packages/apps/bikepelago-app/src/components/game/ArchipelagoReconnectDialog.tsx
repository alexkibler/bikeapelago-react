import React from 'react';
import { Loader2 } from 'lucide-react';
import ModalShell from '../layout/ModalShell';
import { useArchipelagoStore } from '../../store/archipelagoStore';

interface ArchipelagoReconnectDialogProps {
  error: string;
  initialUrl: string;
  initialSlot: string;
  onRetry: (url: string, slot: string, password: string) => void;
  onCancel: () => void;
}

const ArchipelagoReconnectDialog = ({
  error,
  initialUrl,
  initialSlot,
  onRetry,
  onCancel,
}: ArchipelagoReconnectDialogProps) => {
  const [url, setUrl] = React.useState(initialUrl);
  const [slot, setSlot] = React.useState(initialSlot);
  const [password, setPassword] = React.useState('');
  const { status: apStatus } = useArchipelagoStore();
  const isRetrying = apStatus === 'connecting';

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (url.trim() && slot.trim()) onRetry(url.trim(), slot.trim(), password);
  };

  const inputClass = "w-full px-3 py-2 rounded-lg bg-[rgb(var(--color-surface-overlay))] border border-[var(--color-border-hex)] text-xs text-[var(--color-text-hex)] placeholder:text-[var(--color-text-muted-hex)] focus:outline-none focus:border-[var(--color-primary-hex)] transition-colors";

  return (
    <ModalShell onBackdropClick={onCancel} disableBackdropClick={isRetrying}>
      <div className="h-1 w-full bg-[var(--color-error-hex)]" />
      <div className="p-6" aria-labelledby="ap-reconnect-title">
          {/* Header */}
          <div className="flex items-start gap-3 mb-5">
            <div className="flex-shrink-0 w-9 h-9 rounded-full bg-[var(--color-error-hex)]/15 flex items-center justify-center">
              <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-[var(--color-error-hex)]">
                <circle cx="12" cy="12" r="10"/>
                <line x1="12" y1="8" x2="12" y2="12"/>
                <line x1="12" y1="16" x2="12.01" y2="16"/>
              </svg>
            </div>
            <div className="flex-1 min-w-0">
              <h2 id="ap-reconnect-title" className="text-sm font-bold text-[var(--color-text-hex)] mb-0.5">Archipelago Connection Failed</h2>
              <p className="text-[10px] text-[var(--color-text-muted-hex)] leading-relaxed break-words">{error}</p>
            </div>
          </div>

          {/* Reconnect form */}
          <form onSubmit={handleSubmit} className="space-y-3">
            <div>
              <label htmlFor="ap-reconnect-url" className="block text-[10px] font-bold text-[var(--color-text-muted-hex)] uppercase tracking-wider mb-1">Server URL</label>
              <input
                id="ap-reconnect-url"
                type="text"
                value={url}
                onChange={e => setUrl(e.target.value)}
                placeholder="archipelago.gg:12345"
                className={inputClass}
                disabled={isRetrying}
                autoFocus
              />
            </div>
            <div>
              <label htmlFor="ap-reconnect-slot" className="block text-[10px] font-bold text-[var(--color-text-muted-hex)] uppercase tracking-wider mb-1">Slot Name</label>
              <input
                id="ap-reconnect-slot"
                type="text"
                value={slot}
                onChange={e => setSlot(e.target.value)}
                placeholder="YourSlotName"
                className={inputClass}
                disabled={isRetrying}
              />
            </div>
            <div>
              <label htmlFor="ap-reconnect-password" className="block text-[10px] font-bold text-[var(--color-text-muted-hex)] uppercase tracking-wider mb-1">Password <span className="normal-case font-normal">(optional)</span></label>
              <input
                id="ap-reconnect-password"
                type="password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                placeholder="Leave blank if none"
                className={inputClass}
                disabled={isRetrying}
              />
            </div>

            <div className="flex gap-2 pt-1">
              <button
                type="button"
                onClick={onCancel}
                disabled={isRetrying}
                className="flex-1 px-4 py-2 text-xs font-bold rounded-lg border border-[var(--color-border-hex)] text-[var(--color-text-muted-hex)] hover:bg-[rgb(var(--color-surface-overlay))] transition-colors disabled:opacity-40"
              >
                Cancel
              </button>
              <button
                id="ap-reconnect-submit"
                type="submit"
                disabled={isRetrying || !url.trim() || !slot.trim()}
                className="flex-1 px-4 py-2 text-xs font-bold rounded-lg bg-[var(--color-primary-hex)] hover:opacity-90 text-white transition-opacity active:scale-95 disabled:opacity-50 flex items-center justify-center gap-2"
              >
                {isRetrying ? (
                  <><Loader2 className="w-3 h-3 animate-spin" /> Connecting…</>
                ) : 'Try Again'}
              </button>
            </div>
          </form>
      </div>
    </ModalShell>
  );
};

export default ArchipelagoReconnectDialog;
