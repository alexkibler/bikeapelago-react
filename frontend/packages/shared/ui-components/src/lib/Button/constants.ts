import type { Values } from '@bikeapelago/shared-util-types';

export const ButtonSizes = {
  sm: 'sm',
  md: 'md',
  lg: 'lg',
};

export type ButtonSize = Values<typeof ButtonSizes>;

export const ButtonVariants = {
  default: 'default'
};

export type ButtonVariant = Values<typeof ButtonVariants>;
