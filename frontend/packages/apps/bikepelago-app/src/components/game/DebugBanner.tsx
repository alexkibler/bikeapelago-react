import { useState } from 'react';
import { useDebugStore } from '../../store/debugStore';
import { useGameStore } from '../../store/gameStore';
import { getToken } from '../../store/authStore';

const DebugBanner = () => {
  const { debugMode, toggle } = useDebugStore();
  const nodes = useGameStore((s) => s.nodes);
  const setNodes = useGameStore((s) => s.setNodes);
  const session = useGameStore((s) => s.session);
  const triggerSync = useGameStore((s) => s.triggerSync);
  const [clearing, setClearing] = useState(false);
  const [completing, setCompleting] = useState(false);

  if (!debugMode) return null;

  const availableNodes = nodes.filter((n) => n.state === 'Available');

  const handleForceComplete = async () => {
    if (completing || !session) return;
    setCompleting(true);
    try {
      const token = getToken();
      const res = await fetch(`/api/sessions/${session.id}/debug/force-complete`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
      });
      if (res.ok) {
        setNodes(nodes.map((n) => ({ ...n, state: 'Checked' as const })));
        triggerSync();
      }
    } finally {
      setCompleting(false);
    }
  };

  const handleClearAll = async () => {
    if (clearing || availableNodes.length === 0 || !session) return;
    setClearing(true);
    try {
      const token = getToken();
      const res = await fetch(`/api/sessions/${session.id}/nodes/check`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({ nodeIds: availableNodes.map((n) => n.id) }),
      });
      if (res.ok) {
        setNodes(nodes.map((n) => n.state === 'Available' ? { ...n, state: 'Checked' as const } : n));
        triggerSync();
      }
    } finally {
      setClearing(false);
    }
  };

  return (
    <div className="fixed bottom-16 md:bottom-0 left-0 right-0 z-[2000] bg-yellow-400 px-4 py-2 flex items-center justify-between">
      <span className="text-[11px] font-black uppercase tracking-widest text-black">
        ⚠ Debug Mode
      </span>
      <div className="flex items-center gap-4">
        <button
          onClick={handleForceComplete}
          disabled={completing || !session}
          className="text-[11px] font-black uppercase tracking-widest text-black underline underline-offset-2 hover:opacity-60 transition-opacity disabled:opacity-40 disabled:no-underline"
        >
          {completing ? 'Completing…' : 'Force Complete'}
        </button>
        <button
          onClick={handleClearAll}
          disabled={clearing || availableNodes.length === 0}
          className="text-[11px] font-black uppercase tracking-widest text-black underline underline-offset-2 hover:opacity-60 transition-opacity disabled:opacity-40 disabled:no-underline"
        >
          {clearing ? 'Clearing…' : `Clear All (${availableNodes.length})`}
        </button>
        <button
          onClick={toggle}
          className="text-[11px] font-black uppercase tracking-widest text-black underline underline-offset-2 hover:opacity-60 transition-opacity"
        >
          Disable
        </button>
      </div>
    </div>
  );
};

export default DebugBanner;
