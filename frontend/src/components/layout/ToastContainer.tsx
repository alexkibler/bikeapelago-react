import React from 'react';
import { useToastStore, type ToastType } from '../../store/toastStore';
import { CheckCircle, XCircle, AlertCircle, Info, X } from 'lucide-react';

const ToastIcon = ({ type }: { type: ToastType }) => {
  switch (type) {
    case 'success': return <CheckCircle className="w-5 h-5 text-green-500" />;
    case 'error': return <XCircle className="w-5 h-5 text-red-500" />;
    case 'warning': return <AlertCircle className="w-5 h-5 text-yellow-500" />;
    case 'info': return <Info className="w-5 h-5 text-blue-500" />;
  }
};

const ToastContainer = () => {
  const { toasts, removeToast } = useToastStore();

  return (
    <div className="fixed bottom-24 md:bottom-8 right-6 z-[3000] flex flex-col gap-3 pointer-events-none">
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={`
            pointer-events-auto
            flex items-center gap-3 px-5 py-4 rounded-2xl shadow-2xl border backdrop-blur-xl
            animate-in fade-in slide-in-from-right-8 duration-300
            ${toast.type === 'success' ? 'bg-green-500/10 border-green-500/20 text-white' : ''}
            ${toast.type === 'error' ? 'bg-red-500/10 border-red-500/20 text-white' : ''}
            ${toast.type === 'warning' ? 'bg-yellow-500/10 border-yellow-500/20 text-white' : ''}
            ${toast.type === 'info' ? 'bg-blue-500/10 border-blue-500/20 text-white' : ''}
          `}
        >
          <ToastIcon type={toast.type} />
          <span className="text-sm font-bold tracking-tight">{toast.message}</span>
          <button 
            onClick={() => removeToast(toast.id)}
            className="ml-2 p-1 rounded-full hover:bg-white/10 transition-colors text-neutral-500 hover:text-white"
            aria-label="Close toast"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      ))}
    </div>
  );
};

export default ToastContainer;
