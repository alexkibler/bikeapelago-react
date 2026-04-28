import React, { useEffect, useRef, useState } from 'react';

import { AlertCircle, Info, MessageSquare, Send, Zap } from 'lucide-react';

import { archipelago } from '../../lib/archipelago';
import { useArchipelagoStore } from '../../store/archipelagoStore';
import type { ChatMessage } from '../../store/archipelagoStore';

const ChatPanel = () => {
  const { messages, status, status: connectionStatus } = useArchipelagoStore();
  const [inputValue, setInputValue] = useState('');
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const handleSendMessage = (e: React.FormEvent) => {
    e.preventDefault();
    if (!inputValue.trim()) return;

    if (connectionStatus === 'connected') {
      void archipelago.say(inputValue.trim());
      setInputValue('');
    }
  };

  const getMessageIcon = (type: ChatMessage['type']) => {
    switch (type) {
      case 'item':
        return <Zap className='w-3 h-3 text-[var(--color-primary-hex)]' />;
      case 'error':
        return (
          <AlertCircle className='w-3 h-3 text-[var(--color-error-hex)]' />
        );
      case 'player':
        return (
          <MessageSquare className='w-3 h-3 text-[var(--color-info-hex)]' />
        );
      default:
        return <Info className='w-3 h-3 text-[var(--color-text-subtle-hex)]' />;
    }
  };

  const getMessageColor = (type: ChatMessage['type']) => {
    switch (type) {
      case 'item':
        return 'text-[var(--color-primary-hex)]';
      case 'error':
        return 'text-[var(--color-error-hex)]';
      case 'player':
        return 'text-[var(--color-info-hex)]';
      case 'system':
        return 'text-[var(--color-text-muted-hex)]';
      default:
        return 'text-[var(--color-text-hex)]';
    }
  };

  return (
    <div className='flex flex-col h-full bg-[var(--color-surface-hex)] border-l border-[var(--color-border-hex)]'>
      <div className='p-4 border-b border-[var(--color-border-hex)] flex items-center justify-between bg-[rgb(var(--color-surface-overlay))]'>
        <div className='flex items-center gap-2'>
          <MessageSquare className='w-4 h-4 text-[var(--color-primary-hex)]' />
          <span className='text-xs font-black uppercase tracking-widest text-[var(--color-text-muted-hex)]'>
            Game Log
          </span>
        </div>
        <div className='flex items-center gap-1.5 px-2 py-0.5 rounded-full bg-[var(--color-surface-hex)]/40 border border-[var(--color-border-hex)]'>
          <div
            className={`w-1.5 h-1.5 rounded-full ${status === 'connected' ? 'bg-[var(--color-success-hex)] shadow-[0_0_8px_rgba(var(--color-success),0.5)]' : 'bg-[var(--color-border-hex)] animate-pulse'}`}
          ></div>
          <span className='text-[10px] font-bold text-[var(--color-text-subtle-hex)] uppercase tracking-tighter'>
            {status}
          </span>
        </div>
      </div>

      <div className='flex-1 overflow-y-auto p-4 space-y-3 custom-scrollbar'>
        {messages.length === 0 && (
          <div className='h-full flex flex-col items-center justify-center opacity-30 text-center px-8'>
            <MessageSquare className='w-8 h-8 mb-3' />
            <p className='text-xs italic'>
              No entries yet. Clearing nodes and receiving items will populate
              this log.
            </p>
          </div>
        )}

        {messages.map((msg) => (
          <div
            key={msg.id}
            className='flex gap-2 group animate-in fade-in slide-in-from-left-2 duration-300'
          >
            <div className='mt-1 shrink-0'>{getMessageIcon(msg.type)}</div>
            <div className='flex flex-col min-w-0'>
              <span
                className={`text-[13px] leading-relaxed break-words ${getMessageColor(msg.type)}`}
              >
                {msg.text}
              </span>
              <span className='text-[9px] text-[var(--color-text-subtle-hex)] font-medium tracking-tighter uppercase mt-0.5 opacity-0 group-hover:opacity-100 transition-opacity'>
                {new Date(msg.timestamp).toLocaleTimeString([], {
                  hour: '2-digit',
                  minute: '2-digit',
                })}
              </span>
            </div>
          </div>
        ))}
        <div ref={messagesEndRef} />
      </div>

      <div className='p-4 border-t border-[var(--color-border-hex)] bg-[var(--color-surface-hex)]/20'>
        <form onSubmit={handleSendMessage} className='relative flex gap-2'>
          <input
            type='text'
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
            disabled={status !== 'connected'}
            placeholder={
              status === 'connected'
                ? 'Type a message or command...'
                : 'Waiting for connection...'
            }
            className='flex-1 bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] rounded-xl px-4 py-2.5 text-sm text-[var(--color-text-hex)] focus:outline-none focus:ring-1 focus:ring-[var(--color-primary-hex)]/50 disabled:opacity-50 transition-all'
          />
          <button
            type='submit'
            disabled={status !== 'connected' || !inputValue.trim()}
            className='w-10 h-10 bg-[var(--color-primary-hex)] hover:bg-[var(--color-primary-hover-hex)] disabled:opacity-30 disabled:bg-[var(--color-surface-alt-hex)] rounded-xl flex items-center justify-center transition-all shadow-lg active:scale-95'
          >
            <Send className='w-4 h-4 text-white' />
          </button>
        </form>
      </div>
    </div>
  );
};

export default ChatPanel;
