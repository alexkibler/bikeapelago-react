import type { ButtonSize, ButtonVariant } from './constants';

export type ButtonProps = {
  buttonSize?: ButtonSize;
  className?: string;
  hasErrors?: boolean;
  isLoading?: boolean;
  variant?: ButtonVariant;
}
