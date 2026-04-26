import type { JSX } from 'react';

import { Loader2 } from 'lucide-react';
import classnames from 'classnames';
import type { ButtonProps } from './types';
import type { ButtonSize, ButtonVariant } from './constants';
import { twMerge } from 'tailwind-merge'

const sizeClasses: Record<ButtonSize, string> = {
  sm: 'rounded-xl',
  md: 'h-16 rounded-2xl text-lg shadow-xl tracking-widest gap-3 active:scale-[0.98]',
  lg: 'rounded-4xl'
}

const variantClasses: Record<ButtonVariant, string> = {
  default: 'shadow-orange-600/20 bg-[var(--color-primary-hex)] text-white font-black hover:bg-[var(--color-primary-hover-hex)]',
}

export function Button({
  children,
  className,
  hasErrors,
  disabled,
  isLoading,
  buttonSize = 'md',
  variant = 'default',
  ...buttonProps
}: ButtonProps & JSX.IntrinsicElements["button"]) {
  // we'll have more variants later
  const baseClasses = classnames(
    "w-full flex items-center justify-center transition-all disabled:opacity-50",
    variantClasses[variant],
    sizeClasses[buttonSize],
  );

  const errorClasses = classnames({
    'border-bikepelago-button-error-border': hasErrors,
  })

  const buttonClasses = twMerge(baseClasses, errorClasses, className);

  return (
    <button
      className={buttonClasses}
      disabled={isLoading ?? disabled}
      {...buttonProps}
    >
      <div className="flex flex-row">
        {isLoading && <Loader2 className='w-6 h-6 animate-spin mr-2' />}
        <div className="w-full flex items-center justify-center">
          {children}
        </div>
      </div>
    </button>
  )
}
