import type { ReactNode } from 'react';

interface ModalShellProps {
  children: ReactNode;
  onBackdropClick?: () => void;
  disableBackdropClick?: boolean;
}

const ModalShell = ({ children, onBackdropClick, disableBackdropClick = false }: ModalShellProps) => {
  return (
    <div
      className='fixed inset-0 z-[2000] flex items-center justify-center p-4'
      role='dialog'
      aria-modal='true'
    >
      {/* Backdrop */}
      <div
        className='absolute inset-0 bg-black/60 backdrop-blur-md transition-opacity duration-300'
        onClick={!disableBackdropClick ? onBackdropClick : undefined}
        aria-hidden='true'
      />

      {/* Panel */}
      <div className='relative w-full max-w-sm bg-[var(--color-surface-hex)] border border-[var(--color-border-hex)] rounded-2xl shadow-2xl overflow-hidden animate-in fade-in zoom-in duration-200'>
        {children}
      </div>
    </div>
  );
};

export default ModalShell;
