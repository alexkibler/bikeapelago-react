import React, { useState, useRef } from 'react';
import { useGameStore } from '../../store/gameStore';
import { Upload, FileCheck, Loader2, Play, CheckCircle2, XCircle } from 'lucide-react';
import { useFitAnalyzer } from '../../hooks/useFitAnalyzer';

const UploadPanel = ({ sessionId }: { sessionId: string }) => {
  const { analysisResult, setAnalysisResult } = useGameStore();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const {
    analyzeFile,
    confirmValidation,
    loading,
    error,
    setError,
    success,
    setSuccess
  } = useFitAnalyzer(sessionId, setAnalysisResult);

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      setSelectedFile(file);
      setError(null);
      setSuccess(null);
    }
  };

  const handleAnalyze = () => analyzeFile(selectedFile);
  const handleConfirm = () => confirmValidation(analysisResult);

  return (
    <div className="flex flex-col h-full bg-neutral-900 text-white p-4 overflow-y-auto">
      <div className="flex items-center gap-2 mb-6 text-xl font-bold">
        <Upload className="w-5 h-5 text-orange-500" />
        <h2 className="panel-title">Upload Ride</h2>
      </div>

      {!analysisResult ? (
        <div className="flex-1 flex flex-col items-center justify-center border-2 border-dashed border-white/10 rounded-2xl p-8 hover:border-orange-500/50 transition-all group bg-white/5">
          <input 
            type="file" 
            id="file-upload" 
            accept=".fit" 
            className="hidden" 
            onChange={handleFileChange} 
            ref={fileInputRef}
          />
          {!selectedFile ? (
            <>
              <button 
                onClick={() => fileInputRef.current?.click()}
                className="w-16 h-16 rounded-full bg-orange-500/10 text-orange-500 flex items-center justify-center mb-4 group-hover:scale-110 transition-transform"
              >
                <Play className="w-6 h-6 rotate-[-90deg] translate-y-[-2px]" />
              </button>
              <p className="text-sm font-bold text-neutral-300">Choose a .FIT file</p>
              <p className="text-xs text-neutral-500 mt-2">Maximum file size: 10MB</p>
            </>
          ) : (
            <div className="flex flex-col items-center gap-4">
               <div className="p-4 bg-orange-500/10 rounded-xl flex items-center gap-3 border border-orange-500/20">
                  <FileCheck className="w-6 h-6 text-orange-500" />
                  <span className="text-sm font-medium">{selectedFile.name}</span>
               </div>
               {!loading && (
                 <button 
                   onClick={handleAnalyze}
                   className="px-6 py-2 bg-orange-600 hover:bg-orange-500 rounded-lg text-sm font-bold text-white transition-colors"
                 >
                   Analyze Ride
                 </button>
               )}
            </div>
          )}
          
          {loading && (
            <div className="mt-8 flex flex-col items-center">
              <Loader2 className="w-8 h-8 animate-spin text-orange-500 mb-2" />
              <span className="text-xs text-neutral-400">Analyzing GPS Data...</span>
            </div>
          )}

          {error && (
            <div className="mt-8 p-3 bg-red-500/10 border border-red-500/20 rounded-lg text-red-500 text-xs flex items-center gap-2">
              <XCircle className="w-4 h-4" />
              {error}
            </div>
          )}

          {success && (
            <div className="mt-8 p-4 bg-green-500/10 border border-green-500/20 rounded-lg text-green-400 text-sm flex items-center gap-2 text-center">
              <CheckCircle2 className="w-5 h-5" />
              {success}
            </div>
          )}
        </div>
      ) : (
        <div className="flex-1 flex flex-col">
          <div className="grid grid-cols-2 gap-4 mb-6">
            <div className="bg-white/5 p-4 rounded-xl border border-white/10">
               <span className="text-[10px] font-bold text-neutral-500 uppercase tracking-widest">DISTANCE</span>
               <div className="text-lg font-black mt-1">{(analysisResult.stats.distanceMeters / 1000).toFixed(2)}<span className="text-xs font-normal text-neutral-500 ml-1">km</span></div>
            </div>
            <div className="bg-white/5 p-4 rounded-xl border border-white/10">
               <span className="text-[10px] font-bold text-neutral-500 uppercase tracking-widest">DURATION</span>
               <div className="text-lg font-black mt-1">{Math.floor(analysisResult.stats.durationSeconds / 60)}<span className="text-xs font-normal text-neutral-500 ml-1">min</span></div>
            </div>
          </div>

          <div className="flex-1 overflow-y-auto mb-6">
            <h3 className="text-[10px] font-bold text-neutral-500 uppercase tracking-widest mb-4">Locations Reached</h3>
            {analysisResult.newlyCheckedNodes.length > 0 ? (
              <div className="space-y-3">
                {analysisResult.newlyCheckedNodes.map((node) => (
                  <div key={node.id} className="flex items-center gap-3 bg-green-500/10 p-4 rounded-xl border border-green-500/20 text-green-400">
                     <CheckCircle2 className="w-5 h-5 shrink-0" />
                     <div className="flex flex-col">
                        <span className="font-bold text-sm">Location {node.ap_location_id}</span>
                        <span className="text-[10px] opacity-70">[{node.lat.toFixed(5)}, {node.lon.toFixed(5)}]</span>
                     </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-12 bg-neutral-900/50 rounded-2xl border border-white/5">
                <FileCheck className="w-12 h-12 text-neutral-700 mx-auto mb-4" />
                <p className="text-neutral-500 text-sm">No locations reached in this ride.</p>
              </div>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
             <button 
               onClick={() => setAnalysisResult(null)}
               className="bg-white/5 hover:bg-white/10 p-4 rounded-xl font-bold transition-all border border-white/10"
             >
               Cancel
             </button>
             <button 
               onClick={handleConfirm}
               disabled={analysisResult.newlyCheckedNodes.length === 0 || loading}
               className="bg-green-600 hover:bg-green-500 disabled:opacity-30 disabled:hover:bg-green-600 p-4 rounded-xl font-bold transition-all shadow-lg active:scale-[0.98] flex items-center justify-center gap-2"
             >
               {loading ? <Loader2 className="w-5 h-5 animate-spin" /> : 'Confirm & Send'}
             </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default UploadPanel;
