interface StatProps {
  label: string;
  value: string | number;
  unit?: string;
}

const Stat = ({ label, value, unit }: StatProps) => (
  <div className="flex flex-col">
    <span className="text-[10px] font-bold text-[var(--color-text-muted-hex)] tracking-wider uppercase">{label}</span>
    <span className="text-[var(--color-text-hex)] font-bold text-lg leading-none">
      {value}
      {unit && <span className="text-xs text-[var(--color-text-muted-hex)] font-normal ml-1">{unit}</span>}
    </span>
  </div>
);

export default Stat;
