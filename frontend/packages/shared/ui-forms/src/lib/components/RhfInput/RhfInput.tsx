import type { ChangeEvent, JSX } from 'react';
import { useId } from 'react';

import classnames from 'classnames';
import { get } from 'lodash-es';
import type { FieldValues, Path, PathValue } from 'react-hook-form';
import { twMerge } from 'tailwind-merge';

import type { Size } from '../../shared';
import type { RhfInputProps } from './types';

const sizeClasses: Record<Size, string> = {
  sm: 'rounded-xl',
  md: 'font-medium rounded-2xl py-4 pr-4',
  lg: 'rounded-4xl',
};

export function RhfInput<
  TForm extends FieldValues,
  TField extends Path<TForm>,
>({
  className,
  formMethods,
  label,
  leftIcon,
  name,
  onChange,
  placeholder,
  registerOptions,
  required = false,
  inputSize = 'md',
  type = 'text',
  ...inputProps
}: RhfInputProps<TForm, TField> & JSX.IntrinsicElements['input']) {
  const registerProps = formMethods.register(name, {
    required,
    onChange: (e: ChangeEvent<HTMLInputElement>) => {
      onChange?.(e.target.value as PathValue<TForm, TField>);
    },
    ...(registerOptions ?? {}),
  });
  const inputErrors = get(formMethods.formState.errors, name);
  const hasErrors = !!inputErrors;
  const hasIcon = !!leftIcon;
  const id = useId();
  // we will likely end up with other variants. for now the two I'm working with are the same variant so this is just true
  const variantClasses = classnames({
    'bg-[var(--color-surface-alt-hex)] border-[var(--color-border-hex)] text-[var(--color-text-hex)] focus:border-orange-500/50 placeholder:text-[var(--color-text-subtle-hex)]': true,
  });
  const iconClasses = classnames({
    'pl-12': hasIcon,
  });
  const baseInputClasses = classnames(
    'w-full border shadow rounded focus:outline-none transition-all',
    variantClasses,
    iconClasses,
    sizeClasses[inputSize],
  );

  const errorClasses = classnames({
    'border-bikepelago-input-error-border': hasErrors,
  });

  const inputClasses = twMerge(baseInputClasses, errorClasses, className);

  return (
    <>
      {label && (
        <label
          htmlFor={id}
          className='block text-sm font-medium text-bikepelago-body-text mb-1'
        >
          {label}
        </label>
      )}
      <div className='relative group'>
        {leftIcon ?? null}
        <input
          className={inputClasses}
          id={id}
          required={required}
          type={type}
          placeholder={label ?? placeholder}
          {...registerProps}
          {...inputProps}
        />
      </div>
      {/*{hasErrors && (
        <P textColor="bikepelago-text-alert">
          {isString(inputErrors) && <Text>{inputErrors}</Text>}
          {isString(inputErrors.message) && <Text>{inputErrors.message}</Text>}
        </P>
      )}*/}
    </>
  );
}
