import React, { useState } from 'react';
import type { ColumnMetadata } from '../../types/schema';
import { X, Save } from 'lucide-react';
import { SpatialPreview } from './SpatialPreview';

interface GenericDataFormProps {
  table: string;
  columns: ColumnMetadata[];
  initialData?: any;
  onSave: (data: any) => void;
  onCancel: () => void;
}

export const GenericDataForm: React.FC<GenericDataFormProps> = ({ table, columns, initialData, onSave, onCancel }) => {
  const [formData, setFormData] = useState<any>(initialData || {});

  const handleChange = (name: string, value: any) => {
    setFormData((prev: any) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave(formData);
  };

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center p-8 bg-zinc-950/80 backdrop-blur-xl animate-in fade-in duration-300">
      <div className="bg-zinc-900 border border-zinc-800 w-full max-w-2xl rounded-[32px] overflow-hidden shadow-2xl relative z-10 flex flex-col max-h-[90vh]">
        <div className="p-6 border-b border-zinc-800 flex items-center justify-between">
          <div>
            <h3 className="font-bold text-xl text-white">{initialData ? 'Update' : 'Create'} {table} Record</h3>
            <p className="text-zinc-500 text-sm">Fill in the fields defined in schema</p>
          </div>
          <button onClick={onCancel} className="p-2 hover:bg-zinc-800 rounded-xl text-zinc-400">
            <X size={20} />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-8 space-y-6 custom-scrollbar">
          {columns.map((col) => {
            if (col.isPrimaryKey && !initialData) return null; // Don't show PK for new records if auto-gen, or show if required.
            
            return (
              <div key={col.name} className="space-y-2">
                <label className="text-xs font-bold text-zinc-500 uppercase tracking-widest ml-1 flex justify-between">
                   <span>{col.name}</span>
                   <span className="text-[10px] text-zinc-700">{col.type} {col.isNullable ? '(Optional)' : '(Required)'}</span>
                </label>
                
                {col.isSpatial ? (
                  <div className="space-y-4">
                     <textarea
                       className="w-full bg-zinc-950 border border-zinc-800 rounded-2xl p-4 text-zinc-100 focus:ring-2 focus:ring-indigo-500/50 text-xs font-mono"
                       placeholder='{"type": "Point", "coordinates": [0,0]}'
                       value={typeof formData[col.name] === 'string' ? formData[col.name] : JSON.stringify(formData[col.name])}
                       onChange={(e) => {
                         try {
                           const json = JSON.parse(e.target.value);
                           handleChange(col.name, json);
                         } catch {
                           handleChange(col.name, e.target.value);
                         }
                       }}
                       rows={3}
                     />
                     {formData[col.name] && typeof formData[col.name] === 'object' && (
                       <div className="border border-zinc-800 rounded-2xl p-2 bg-zinc-950/20">
                          <p className="text-[10px] font-bold text-zinc-700 uppercase tracking-widest mb-2 ml-2">Quick Preview</p>
                          <SpatialPreview geoJson={formData[col.name]} height="140px" interactive={false} />
                       </div>
                     )}
                  </div>
                ) : col.type === 'boolean' ? (
                  <div className="flex items-center space-x-3 p-4 bg-zinc-950 border border-zinc-800 rounded-2xl">
                     <input
                       type="checkbox"
                       checked={!!formData[col.name]}
                       onChange={(e) => handleChange(col.name, e.target.checked)}
                       className="w-5 h-5 rounded bg-zinc-900 border-zinc-800 text-indigo-500 focus:ring-indigo-500/50"
                     />
                     <span className="text-zinc-400 text-sm">Enabled / True</span>
                  </div>
                ) : col.type === 'number' ? (
                  <input
                    type="number"
                    step="any"
                    className="w-full bg-zinc-950 border border-zinc-800 rounded-2xl p-4 text-zinc-100 focus:ring-2 focus:ring-indigo-500/50"
                    value={formData[col.name] || ''}
                    onChange={(e) => handleChange(col.name, parseFloat(e.target.value))}
                    required={!col.isNullable}
                  />
                ) : (
                  <input
                    type="text"
                    className="w-full bg-zinc-950 border border-zinc-800 rounded-2xl p-4 text-zinc-100 focus:ring-2 focus:ring-indigo-500/50"
                    value={formData[col.name] || ''}
                    onChange={(e) => handleChange(col.name, e.target.value)}
                    required={!col.isNullable && !col.isPrimaryKey}
                  />
                )}
              </div>
            );
          })}
        </form>

        <div className="p-6 border-t border-zinc-800 flex items-center justify-end gap-3 bg-zinc-900/50">
           <button onClick={onCancel} className="px-6 py-3 text-sm font-bold text-zinc-400 hover:text-white transition-all uppercase tracking-widest">
              Discard
           </button>
           <button 
             onClick={handleSubmit}
             className="px-8 py-3 bg-indigo-600 hover:bg-indigo-500 text-white font-black rounded-2xl shadow-xl shadow-indigo-600/20 flex items-center transition-all active:scale-95"
           >
              <Save size={18} className="mr-2" />
              Save Changes
           </button>
        </div>
      </div>
    </div>
  );
};
