import { useDebugStore } from '../../store/debugStore';

const DebugBanner = () => {
  const { debugMode, toggle } = useDebugStore();
  if (!debugMode) return null;

  return (
    <div className="fixed bottom-0 left-0 right-0 z-[2000] bg-yellow-400 px-4 py-2 flex items-center justify-between">
      <span className="text-[11px] font-black uppercase tracking-widest text-black">
        ⚠ Debug Mode — tap an orange node to mark it checked
      </span>
      <button
        onClick={toggle}
        className="text-[11px] font-black uppercase tracking-widest text-black underline underline-offset-2 hover:opacity-60 transition-opacity"
      >
        Disable
      </button>
    </div>
  );
};

export default DebugBanner;
