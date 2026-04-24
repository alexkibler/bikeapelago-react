interface ToggleProps {
  id?: string;
  label?: string;
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  disabled?: boolean;
  className?: string;
}

const Toggle = ({ id, label, checked, onCheckedChange, disabled = false, className = '' }: ToggleProps) => {
  return (
    <div className={`flex items-center justify-between ${className}`}>
      {label && (
        <label
          htmlFor={id}
          onClick={(e) => {
            e.preventDefault();
            if (!disabled) onCheckedChange(!checked);
            document.getElementById(id as string)?.focus();
          }}
          className='text-xs font-bold uppercase tracking-wider text-[var(--color-text-subtle-hex)] cursor-pointer'
        >
          {label}
        </label>
      )}
      <button
        id={id}
        onClick={() => onCheckedChange(!checked)}
        disabled={disabled}
        className={`relative w-10 h-5 rounded-full transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 ${
          checked
            ? 'bg-[var(--color-primary-hex)] focus:ring-[var(--color-primary-hex)]/40'
            : 'bg-[rgb(var(--color-surface-overlay))] focus:ring-[var(--color-border-strong-hex)]'
        }`}
        role='switch'
        aria-checked={checked}
      >
        <div
          className={`absolute top-1 left-1 w-3 h-3 rounded-full bg-white transition-transform duration-200 transform ${
            checked ? 'translate-x-5' : 'translate-x-0'
          }`}
        />
      </button>
    </div>
  );
};

export default Toggle;
