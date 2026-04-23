import type { ReactNode } from 'react';
import { AlertTriangle, Loader2 } from 'lucide-react';
import ModalShell from './ModalShell';

interface ConfirmDialogProps {
  title: string;
  message: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  onConfirm: () => void;
  onCancel: () => void;
  isLoading?: boolean;
}

const ConfirmDialog = ({
  title,
  message,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  onConfirm,
  onCancel,
  isLoading = false,
}: ConfirmDialogProps) => {
  return (
    <ModalShell onBackdropClick={onCancel} disableBackdropClick={isLoading}>
      <div className='h-1.5 w-full bg-red-600' />
      <div className='p-8'>
        <div className='flex items-center justify-center w-14 h-14 rounded-full bg-red-500/10 mb-6 mx-auto'>
          <AlertTriangle className='w-7 h-7 text-red-500' />
        </div>

        <h2 className='text-xl font-bold text-[var(--color-text-hex)] text-center mb-2'>
          {title}
        </h2>
        <p className='text-[var(--color-text-muted-hex)] text-center text-sm mb-8 leading-relaxed'>
          {message}
        </p>

        <div className='flex gap-3'>
          <button
            onClick={onCancel}
            disabled={isLoading}
            className='flex-1 px-4 py-3 text-xs font-black uppercase tracking-widest rounded-xl border border-[var(--color-border-hex)] text-[var(--color-text-muted-hex)] hover:bg-[var(--color-surface-alt-hex)] hover:text-[var(--color-text-hex)] transition-all disabled:opacity-50'
          >
            {cancelLabel}
          </button>
          <button
            onClick={onConfirm}
            disabled={isLoading}
            className='flex-1 px-4 py-3 text-xs font-black uppercase tracking-widest rounded-xl bg-red-600 hover:bg-red-500 text-white shadow-lg shadow-red-900/20 transition-all active:scale-95 disabled:opacity-50 flex items-center justify-center gap-2'
          >
            {isLoading ? (
              <>
                <Loader2 className='w-4 h-4 animate-spin' />
                Loading...
              </>
            ) : (
              confirmLabel
            )}
          </button>
        </div>
      </div>
    </ModalShell>
  );
};

export default ConfirmDialog;
