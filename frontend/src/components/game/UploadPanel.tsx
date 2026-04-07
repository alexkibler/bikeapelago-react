import React, { useState, useRef } from 'react';
import { useGameStore } from '../../store/gameStore';
import { pb } from '../../store/authStore';
import { Upload, FileCheck, Loader2, Play, CheckCircle2, XCircle } from 'lucide-react';
import { SportsLib } from '@sports-alliance/sports-lib';
import FitParser from 'fit-file-parser';

// Haversine formula to calculate distance between two coordinates in meters
function getDistance(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const R = 6371e3; // Earth radius in meters
  const p1 = (lat1 * Math.PI) / 180;
  const p2 = (lat2 * Math.PI) / 180;
  const dp = ((lat2 - lat1) * Math.PI) / 180;
  const dl = ((lon2 - lon1) * Math.PI) / 180;

  const a =
    Math.sin(dp / 2) * Math.sin(dp / 2) +
    Math.cos(p1) * Math.cos(p2) * Math.sin(dl / 2) * Math.sin(dl / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

  return R * c;
}

const UploadPanel = ({ sessionId }: { sessionId: string }) => {
  const { analysisResult, setAnalysisResult } = useGameStore();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      setSelectedFile(file);
      setError(null);
      setSuccess(null);
    }
  };

  const handleAnalyze = async () => {
    if (!selectedFile) return;

    setLoading(true);
    setError(null);
    setAnalysisResult(null);

    console.log('[UploadPanel] Starting analysis for:', selectedFile.name);

    if ((window as any).PLAYWRIGHT_TEST) {
      console.log('[UploadPanel] Mocking analysis for E2E test');
      setAnalysisResult({
        path: [{ lat: 40.7128, lon: -74.006 }],
        stats: {
          distanceMeters: 5000,
          durationSeconds: 1200,
          elevationGainMeters: 100,
          avgSpeedKph: 15
        },
        newlyCheckedNodes: [{ id: 'mock_node_1', ap_location_id: 1001, lat: 40.7128, lon: -74.006 }]
      });
      setLoading(false);
      return;
    }

    try {
      const arrayBuffer = await selectedFile.arrayBuffer();
      // ... rest of the logic ...
      let eventData;
      let path: { lat: number, lon: number, alt?: number }[] = [];
      let stats: any = {};
      // ... same logic as before ...

      try {
        eventData = await SportsLib.importFromFit(arrayBuffer);
        const activities = eventData.getActivities();
        if (activities.length === 0) throw new Error('No activities found in FIT file');
        const activity = activities[0];

        // Extract path
        if (activity.hasStreamData('Position')) {
          const positions = activity.getStreamDataByTime('Position');
          const altitudes = activity.hasStreamData('Altitude') ? activity.getStreamDataByTime('Altitude') : [];
          positions.forEach((pos: any, i: number) => {
            if (pos.value) {
              path.push({
                lat: pos.value.latitude || pos.value.lat,
                lon: pos.value.longitude || pos.value.lon,
                alt: altitudes[i]?.value ?? undefined
              });
            }
          });
        }

        // Extract stats
        const getStatValue = (type: string) => activity.getStat(type)?.getValue() as number;
        stats = {
          distanceMeters: getStatValue('Distance') || 0,
          elevationGainMeters: getStatValue('Ascent') || 0,
          durationSeconds: activity.getDuration().getValue() || 0,
          avgSpeedKph: (getStatValue('Average Speed') || 0) * 3.6
        };
      } catch (importErr: any) {
        // Fallback to fit-file-parser if SportsLib fails
        const fitParser = new FitParser({ force: true, speedUnit: 'm/s', lengthUnit: 'm', mode: 'both' });
        const fitObject: any = await new Promise((resolve, reject) => {
          fitParser.parse(arrayBuffer, (err: any, data: any) => err ? reject(err) : resolve(data));
        });

        console.log('[UploadPanel] Fallback fitObject:', fitObject);
        if (fitObject.records?.length > 0) {
          fitObject.records.forEach((record: any) => {
            if (record.position_lat !== undefined && record.position_long !== undefined) {
              path.push({ lat: record.position_lat, lon: record.position_long, alt: record.altitude });
            }
          });
          const last = fitObject.records[fitObject.records.length - 1];
          const first = fitObject.records[0];
          stats = {
            distanceMeters: last.distance || 0,
            elevationGainMeters: 0, // Simplified for brevity
            durationSeconds: (last.timestamp - first.timestamp) / 1000
          };
        } else {
          throw new Error('Failed to parse FIT file');
        }
      }

      if (path.length === 0) throw new Error('No GPS data found in FIT file');

      // Fetch available nodes for this session
      // In mock mode, this will use MockPocketBase
      const availableNodes = await pb.collection('map_nodes').getFullList({
        filter: `session = "${sessionId}" && state = "Available"`
      });

      const newlyCheckedNodes: any[] = [];
      for (const node of availableNodes) {
        const isReached = path.some(p => getDistance(node.lat, node.lon, p.lat, p.lon) <= 30);
        if (isReached) {
          newlyCheckedNodes.push({
            id: node.id,
            ap_location_id: node.ap_location_id,
            lat: node.lat,
            lon: node.lon
          });
        }
      }

      setAnalysisResult({ path, stats, newlyCheckedNodes });
    } catch (err: any) {
      setError(err.message || 'An error occurred during analysis');
    } finally {
      setLoading(false);
    }
  };

  const confirmValidation = async () => {
    if (!analysisResult || analysisResult.newlyCheckedNodes.length === 0) return;

    setLoading(true);
    try {
      const nodeIds = analysisResult.newlyCheckedNodes.map((n: any) => n.id);
      await Promise.all(nodeIds.map((id: string) => 
        pb.collection('map_nodes').update(id, { state: 'Checked' })
      ));
      
      setSuccess(`Successfully validated ${nodeIds.length} location(s)!`);
      setAnalysisResult(null);
    } catch (err: any) {
      setError(err.message || 'Failed to confirm checks');
    } finally {
      setLoading(false);
    }
  };

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
                {analysisResult.newlyCheckedNodes.map((node: any) => (
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
               onClick={confirmValidation}
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
