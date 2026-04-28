import { X } from 'lucide-react';
import { useParams } from 'react-router-dom';

import { useGameStore } from '../../store/gameStore';
import ChatPanel from './ChatPanel';
import InventoryPanel from './InventoryPanel';
import RoutePanel from './RoutePanel';
import UploadPanel from './UploadPanel';

interface SidePanelCoordinatorProps {
  panel: string;
}

const SidePanelCoordinator = ({ panel }: SidePanelCoordinatorProps) => {
  const { id } = useParams();
  const { setActivePanel } = useGameStore();

  return (
    <>
      {/* Mobile-only backdrop: intercepts map clicks and closes the panel */}
      <div
        className='md:hidden absolute inset-0 z-[9]'
        onClick={() => setActivePanel(null)}
        aria-hidden='true'
      />
      <div className='w-full md:w-96 border-l border-[var(--color-border-hex)] flex flex-col bg-[var(--color-surface-hex)] z-10 absolute bottom-0 left-0 right-0 top-[20%] md:top-0 md:relative md:bg-[var(--color-surface-alt-hex)] shadow-2xl md:shadow-none rounded-t-3xl md:rounded-none overflow-hidden md:pt-12'>
        <div className='flex items-center justify-between p-4 border-b border-[var(--color-border-hex)] md:hidden'>
          <span className='font-bold text-[var(--color-text-hex)] uppercase tracking-widest text-xs'>
            {panel}
          </span>
          <button
            aria-label='Close panel'
            onClick={() => setActivePanel(null)}
            className='p-2 hover:bg-[rgb(var(--color-surface-overlay))] rounded-lg text-[var(--color-text-muted-hex)]'
          >
            <X className='w-5 h-5' />
          </button>
        </div>

        {panel === 'route' && <RoutePanel sessionId={id!} />}
        {panel === 'upload' && <UploadPanel sessionId={id!} />}
        {panel === 'chat' && <ChatPanel />}
        {panel === 'inventory' && <InventoryPanel />}
      </div>
    </>
  );
};

export default SidePanelCoordinator;
