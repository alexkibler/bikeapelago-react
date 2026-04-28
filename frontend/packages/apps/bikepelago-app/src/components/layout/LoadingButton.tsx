import type { ButtonHTMLAttributes, ReactNode } from 'react';

import { Loader2 } from 'lucide-react';

interface LoadingButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  isLoading?: boolean;
  loadingLabel?: string;
  children: ReactNode;
}

const LoadingButton = ({
  isLoading = false,
  loadingLabel = 'Loading...',
  children,
  disabled,
  ...props
}: LoadingButtonProps) => {
  return (
    <button
      {...props}
      disabled={isLoading || disabled}
      className={`flex items-center justify-center gap-2 ${props.className || ''}`}
    >
      {isLoading ? (
        <>
          <Loader2 className='w-4 h-4 animate-spin' />
          {loadingLabel}
        </>
      ) : (
        children
      )}
    </button>
  );
};

export default LoadingButton;
