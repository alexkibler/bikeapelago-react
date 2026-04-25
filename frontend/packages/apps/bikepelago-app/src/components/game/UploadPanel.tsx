import React, { useState, useRef } from 'react';
import { useGameStore } from '../../store/gameStore';
import { useFitImportStore } from '../../store/fitImportStore';
import { Upload, FileCheck, Loader2, Play, CheckCircle2, XCircle } from 'lucide-react';
import { useFitAnalyzer } from '../../hooks/useFitAnalyzer';

const UploadPanel = ({ sessionId }: { sessionId: string }) => {
  const { analysisResult, setAnalysisResult } = useGameStore();
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Consume any FIT file that was opened via the native file-open handler before
  // this panel mounted. Using getState() reads the store synchronously so no
  // effect or cascade render is needed.
  const [selectedFile, setSelectedFile] = useState<File | null>(() => {
    const { pendingFile, setPendingFile } = useFitImportStore.getState();
    if (pendingFile) setPendingFile(null);
    return pendingFile;
  });

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
  
  const handleConfirm = async () => {
    const success = await confirmValidation(analysisResult);
    if (success) {
      // We don't update local node states here anymore.
      // The update will come through SignalR once Archipelago confirms the check.
    }
  };

  return (
    <div className="flex flex-col h-full bg-[var(--color-surface-hex)] text-[var(--color-text-hex)] p-4 overflow-y-auto">
      <div className="flex items-center gap-2 mb-6 text-xl font-bold">
        <Upload className="w-5 h-5 text-[var(--color-primary-hex)]" />
        <h2 className="panel-title">Upload Ride</h2>
      </div>

      {!analysisResult ? (
        <div className="flex-1 flex flex-col items-center justify-center border-2 border-dashed border-[var(--color-border-hex)] rounded-2xl p-8 hover:border-[var(--color-primary-hex)]/50 transition-all group bg-[rgb(var(--color-surface-overlay))]">
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
                className="w-16 h-16 rounded-full bg-[var(--color-primary-hex)]/10 text-[var(--color-primary-hex)] flex items-center justify-center mb-4 group-hover:scale-110 transition-transform"
              >
                <Play className="w-6 h-6 rotate-[-90deg] translate-y-[-2px]" />
              </button>
              <p className="text-sm font-bold text-[var(--color-text-muted-hex)]">Choose a .FIT file</p>
              <p className="text-xs text-[var(--color-text-subtle-hex)] mt-2">Maximum file size: 10MB</p>
            </>
          ) : (
            <div className="flex flex-col items-center gap-4">
               <div className="p-4 bg-[var(--color-primary-hex)]/10 rounded-xl flex items-center gap-3 border border-[var(--color-primary-hex)]/20">
                  <FileCheck className="w-6 h-6 text-[var(--color-primary-hex)]" />
                  <span className="text-sm font-medium">{selectedFile.name}</span>
               </div>
               {!loading && (
                 <button
                   onClick={() => void handleAnalyze()}
                   className="px-6 py-2 bg-[var(--color-primary-hex)] hover:bg-[var(--color-primary-hover-hex)] rounded-lg text-sm font-bold text-white transition-colors"
                 >
                   Analyze Ride
                 </button>
               )}
            </div>
          )}

          {loading && (
            <div className="mt-8 flex flex-col items-center">
              <Loader2 className="w-8 h-8 animate-spin text-[var(--color-primary-hex)] mb-2" />
              <span className="text-xs text-[var(--color-text-muted-hex)]">Analyzing GPS Data...</span>
            </div>
          )}

          {error && (
            <div className="mt-8 p-3 bg-[var(--color-error-hex)]/10 border border-[var(--color-error-hex)]/20 rounded-lg text-[var(--color-error-hex)] text-xs flex items-center gap-2">
              <XCircle className="w-4 h-4" />
              {error}
            </div>
          )}

          {success && (
            <div className="mt-8 p-4 bg-[var(--color-success-hex)]/10 border border-[var(--color-success-hex)]/20 rounded-lg text-[var(--color-success-hex)] text-sm flex items-center gap-2 text-center">
              <CheckCircle2 className="w-5 h-5" />
              {success}
            </div>
          )}
        </div>
      ) : (
        <div className="flex-1 flex flex-col">
          <div className="grid grid-cols-2 gap-4 mb-6">
            <div className="bg-[rgb(var(--color-surface-overlay))] p-4 rounded-xl border border-[var(--color-border-hex)]">
               <span className="text-[10px] font-bold text-[var(--color-text-subtle-hex)] uppercase tracking-widest">DISTANCE</span>
               <div className="text-lg font-black mt-1">{(analysisResult.stats.distanceMeters / 1000).toFixed(2)}<span className="text-xs font-normal text-[var(--color-text-subtle-hex)] ml-1">km</span></div>
            </div>
            <div className="bg-[rgb(var(--color-surface-overlay))] p-4 rounded-xl border border-[var(--color-border-hex)]">
               <span className="text-[10px] font-bold text-[var(--color-text-subtle-hex)] uppercase tracking-widest">DURATION</span>
               <div className="text-lg font-black mt-1">{Math.floor(analysisResult.stats.durationSeconds / 60)}<span className="text-xs font-normal text-[var(--color-text-subtle-hex)] ml-1">min</span></div>
            </div>
          </div>

          <div className="flex-1 overflow-y-auto mb-6">
            <h3 className="text-[10px] font-bold text-[var(--color-text-subtle-hex)] uppercase tracking-widest mb-4">Locations Reached</h3>
            {analysisResult.newlyCheckedNodes.length > 0 ? (
              <div className="space-y-3">
                {analysisResult.newlyCheckedNodes.map((node) => (
                  <div key={node.id} className="flex items-center gap-3 bg-[var(--color-success-hex)]/10 p-4 rounded-xl border border-[var(--color-success-hex)]/20 text-[var(--color-success-hex)]">
                     <CheckCircle2 className="w-5 h-5 shrink-0" />
                     <div className="flex flex-col">
                        <span className="font-bold text-sm">Location {node.apArrivalLocationId ?? node.ap_arrival_location_id}</span>
                        <span className="text-[10px] opacity-70">[{node.lat.toFixed(5)}, {node.lon.toFixed(5)}]</span>
                     </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-12 bg-[var(--color-surface-hex)]/50 rounded-2xl border border-[var(--color-border-hex)]">
                <FileCheck className="w-12 h-12 text-[var(--color-text-subtle-hex)] mx-auto mb-4" />
                <p className="text-[var(--color-text-subtle-hex)] text-sm">No locations reached in this ride.</p>
              </div>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
             <button
               onClick={() => setAnalysisResult(null)}
               className="bg-[rgb(var(--color-surface-overlay))] hover:bg-[rgb(var(--color-surface-overlay))]/[0.08] p-4 rounded-xl font-bold transition-all border border-[var(--color-border-hex)] text-[var(--color-text-hex)]"
             >
               Cancel
             </button>
             <button
               onClick={() => void handleConfirm()}
               disabled={analysisResult.newlyCheckedNodes.length === 0 || loading}
               className="bg-[var(--color-success-hex)] hover:bg-[var(--color-success-hex)]/90 disabled:opacity-30 disabled:hover:bg-[var(--color-success-hex)] p-4 rounded-xl font-bold transition-all shadow-lg active:scale-[0.98] flex items-center justify-center gap-2 text-white"
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
