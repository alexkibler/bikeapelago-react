import React, { useState, useEffect, useRef } from 'react';
import { useArchipelagoStore } from '../../store/archipelagoStore';
import type { ChatMessage } from '../../store/archipelagoStore';
import { archipelago } from '../../lib/archipelago';
import { MessageSquare, Send, Zap, Info, AlertCircle } from 'lucide-react';

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
      archipelago.say(inputValue.trim());
      setInputValue('');
    }
  };

  const getMessageIcon = (type: ChatMessage['type']) => {
    switch (type) {
      case 'item': return <Zap className="w-3 h-3 text-orange-500" />;
      case 'error': return <AlertCircle className="w-3 h-3 text-red-500" />;
      case 'player': return <MessageSquare className="w-3 h-3 text-blue-500" />;
      default: return <Info className="w-3 h-3 text-neutral-500" />;
    }
  };

  const getMessageColor = (type: ChatMessage['type']) => {
    switch (type) {
      case 'item': return 'text-orange-300';
      case 'error': return 'text-red-400';
      case 'player': return 'text-blue-300';
      case 'system': return 'text-neutral-400';
      default: return 'text-white';
    }
  };

  return (
    <div className="flex flex-col h-full bg-neutral-900 border-l border-white/10">
      <div className="p-4 border-b border-white/5 flex items-center justify-between bg-white/5">
        <div className="flex items-center gap-2">
          <MessageSquare className="w-4 h-4 text-orange-500" />
          <span className="text-xs font-black uppercase tracking-widest text-neutral-300">Game Chat</span>
        </div>
        <div className="flex items-center gap-1.5 px-2 py-0.5 rounded-full bg-black/40 border border-white/5">
          <div className={`w-1.5 h-1.5 rounded-full ${status === 'connected' ? 'bg-green-500 shadow-[0_0_8px_rgba(34,197,94,0.5)]' : 'bg-neutral-600 animate-pulse'}`}></div>
          <span className="text-[10px] font-bold text-neutral-500 uppercase tracking-tighter">{status}</span>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto p-4 space-y-3 custom-scrollbar">
        {messages.length === 0 && (
          <div className="h-full flex flex-col items-center justify-center opacity-30 text-center px-8">
            <MessageSquare className="w-8 h-8 mb-3" />
            <p className="text-xs italic">No messages yet. Connect to Archipelago to see game events.</p>
          </div>
        )}
        
        {messages.map((msg) => (
          <div key={msg.id} className="flex gap-2 group animate-in fade-in slide-in-from-left-2 duration-300">
            <div className="mt-1 shrink-0">{getMessageIcon(msg.type)}</div>
            <div className="flex flex-col min-w-0">
              <span className={`text-[13px] leading-relaxed break-words ${getMessageColor(msg.type)}`}>
                {msg.text}
              </span>
              <span className="text-[9px] text-neutral-600 font-medium tracking-tighter uppercase mt-0.5 opacity-0 group-hover:opacity-100 transition-opacity">
                {new Date(msg.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
              </span>
            </div>
          </div>
        ))}
        <div ref={messagesEndRef} />
      </div>

      <div className="p-4 border-t border-white/5 bg-black/20">
        <form onSubmit={handleSendMessage} className="relative flex gap-2">
          <input
            type="text"
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
            disabled={status !== 'connected'}
            placeholder={status === 'connected' ? "Type a message or command..." : "Waiting for connection..."}
            className="flex-1 bg-neutral-800 border border-neutral-700 rounded-xl px-4 py-2.5 text-sm text-white focus:outline-none focus:ring-1 focus:ring-orange-500/50 disabled:opacity-50 transition-all"
          />
          <button
            type="submit"
            disabled={status !== 'connected' || !inputValue.trim()}
            className="w-10 h-10 bg-orange-600 hover:bg-orange-500 disabled:opacity-30 disabled:bg-neutral-800 rounded-xl flex items-center justify-center transition-all shadow-lg active:scale-95"
          >
            <Send className="w-4 h-4 text-white" />
          </button>
        </form>
      </div>
    </div>
  );
};

export default ChatPanel;
