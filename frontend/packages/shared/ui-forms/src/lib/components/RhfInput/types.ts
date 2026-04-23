import type { ReactNode } from 'react';
import type { FieldValues, Path, RegisterOptions, UseFormReturn } from "react-hook-form";
import type { Size } from '../../shared'

export type RhfInputProps<TForm extends FieldValues, TField extends Path<TForm>> = {
  className?: string;
  label?: string;
  leftIcon?: ReactNode;
  placeholder?: string;
  formMethods: UseFormReturn<TForm>;
  name: TField;
  onChange?: (arg: string) => void;
  registerOptions?: RegisterOptions<TForm, TField>;
  required?: boolean;
  inputSize?: Size;
  type?: "text" | "password";
};
