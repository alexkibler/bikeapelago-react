import React, { useState } from 'react';
import { Download, FileCode, CheckCircle, Info, ArrowRight, Server, UploadCloud } from 'lucide-react';

const YamlCreator = () => {
  const [slotName, setSlotName] = useState('');
  const [checkCount, setCheckCount] = useState(10);
  const [isDownloading, setIsDownloading] = useState(false);

  const handleDownloadYaml = () => {
    setIsDownloading(true);
    const yaml = [
      `game: Bikeapelago`,
      `name: ${slotName || 'BikePlayer'}`,
      ``,
      `Bikeapelago:`,
      `  check_count: ${checkCount}`,
      `  goal_type: all_intersections`
    ].join('\n');

    const blob = new Blob([yaml], { type: 'text/yaml' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${slotName || 'BikePlayer'}.yaml`;
    a.click();
    URL.revokeObjectURL(url);
    
    setTimeout(() => setIsDownloading(false), 1000);
  };

  return (
    <div className="max-w-4xl mx-auto py-12 px-6">
      <header className="mb-12 text-center">
        <div className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-orange-500/10 border border-orange-500/20 text-orange-500 text-xs font-bold uppercase tracking-[0.2em] mb-6">
          <Server className="w-3 h-3" />
          Setup Guide
        </div>
        <h1 className="text-white text-4xl font-black mb-4 tracking-tight">Archipelago Configuration</h1>
        <p className="text-neutral-400 max-w-2xl mx-auto">
          Generate the necessary files to host your own Bikeapelago multiworld session.
        </p>
      </header>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
        {/* Step 1: YAML Config */}
        <div className="bg-neutral-900 border border-neutral-800 rounded-3xl p-8 space-y-8 shadow-xl">
          <div className="flex items-center gap-4">
            <div className="w-10 h-10 rounded-xl bg-orange-600/10 border border-orange-500/20 flex items-center justify-center font-black text-orange-500">1</div>
            <h2 className="text-xl font-bold text-white">Configure YAML</h2>
          </div>

          <div className="space-y-6">
            <div className="space-y-2">
              <label className="text-xs font-black uppercase tracking-widest text-neutral-500 ml-1">Slot Name</label>
              <input
                type="text"
                value={slotName}
                onChange={(e) => setSlotName(e.target.value)}
                placeholder="e.g. SpeedRacer"
                className="w-full bg-neutral-800 border border-neutral-700 rounded-xl py-4 px-4 text-white focus:outline-none focus:ring-2 focus:ring-orange-500/50 transition-all placeholder:text-neutral-600"
              />
            </div>

            <div className="space-y-2">
              <label className="text-xs font-black uppercase tracking-widest text-neutral-500 ml-1">Check Count ({checkCount})</label>
              <input
                type="range"
                min="5"
                max="100"
                step="5"
                value={checkCount}
                onChange={(e) => setCheckCount(Number(e.target.value))}
                className="range range-primary range-sm"
              />
              <div className="flex justify-between text-[10px] text-neutral-600 font-bold uppercase tracking-tighter pt-1">
                <span>Casual (5)</span>
                <span>Pro (100)</span>
              </div>
            </div>

            <button
              onClick={handleDownloadYaml}
              disabled={!slotName}
              className="w-full btn btn-orange btn-lg h-14 rounded-2xl gap-3 font-black uppercase tracking-widest text-xs disabled:bg-neutral-800 disabled:text-neutral-600"
            >
              <Download className={`w-4 h-4 ${isDownloading ? 'animate-bounce' : ''}`} />
              {isDownloading ? 'Generating...' : 'Download YAML'}
            </button>
          </div>
        </div>

        {/* Step 2: APWorld & Instructions */}
        <div className="space-y-8">
          <div className="bg-neutral-900 border border-neutral-800 rounded-3xl p-8 shadow-xl">
            <div className="flex items-center gap-4 mb-6">
              <div className="w-10 h-10 rounded-xl bg-orange-600/10 border border-orange-500/20 flex items-center justify-center font-black text-orange-500">2</div>
              <h2 className="text-xl font-bold text-white">Get APWorld</h2>
            </div>
            <p className="text-sm text-neutral-400 mb-6 leading-relaxed">
              Download the game definition file and place it in your Archipelago server's <code>lib/worlds</code> directory.
            </p>
            <button className="w-full btn btn-neutral btn-lg h-14 rounded-2xl gap-3 font-black uppercase tracking-widest text-xs border-neutral-700 hover:bg-neutral-800">
              <FileCode className="w-4 h-4 text-orange-500" />
              bikeapelago.apworld
            </button>
          </div>

          <div className="bg-orange-500/5 border border-orange-500/10 rounded-3xl p-8">
            <div className="flex items-center gap-4 mb-6">
              <div className="w-10 h-10 rounded-xl bg-orange-600/20 flex items-center justify-center font-black text-orange-500">3</div>
              <h2 className="text-xl font-bold text-white">Upload & Play</h2>
            </div>
            <ul className="space-y-4">
              {[
                'Visit your Archipelago host',
                'Upload your YAML file',
                'Ensure APWorld is installed',
                'Start the multiworld generation'
              ].map((step, i) => (
                <li key={i} className="flex items-center gap-3 text-sm text-neutral-400">
                  <CheckCircle className="w-4 h-4 text-orange-500 shrink-0" />
                  {step}
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>

      <div className="mt-12 p-6 bg-neutral-900/50 rounded-2xl border border-neutral-800 flex items-start gap-4">
        <Info className="w-5 h-5 text-neutral-500 shrink-0 mt-0.5" />
        <p className="text-xs text-neutral-500 leading-relaxed">
          The YAML file contains your player preferences, while the APWorld file tells Archipelago how Bikeapelago works. Both are essential for a successful multiworld generation. Once generated, use the connection info to join a session on the Home screen.
        </p>
      </div>
    </div>
  );
};

export default YamlCreator;
