import React, { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { MapContainer, TileLayer, Marker, Circle, useMapEvents, useMap } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { Search, Navigation, Play, Loader2, AlertCircle } from 'lucide-react';
import { pb, useAuthStore } from '../store/authStore';


// Fix Leaflet marker icon issue
import markerIcon from 'leaflet/dist/images/marker-icon.png';
import markerShadow from 'leaflet/dist/images/marker-shadow.png';

const DefaultIcon = L.icon({
  iconUrl: markerIcon,
  shadowUrl: markerShadow,
  iconSize: [25, 41],
  iconAnchor: [12, 41]
});
L.Marker.prototype.options.icon = DefaultIcon;

const MapEvents = ({ onMapClick }: { onMapClick: (lat: number, lng: number) => void }) => {
  useMapEvents({
    click(e) {
      onMapClick(e.latlng.lat, e.latlng.lng);
    },
  });
  return null;
};

// Force map to recalculate its size after mounting (fixes blank tile issue)
const MapResizer = () => {
  const map = useMap();
  useEffect(() => {
    setTimeout(() => map.invalidateSize(), 100);
  }, [map]);
  return null;
};

const SessionSetup = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  
  const serverUrl = searchParams.get('serverUrl') || '';
  const slotName = searchParams.get('slotName') || '';
  const mode = searchParams.get('mode') || 'archipelago';

  const { user } = useAuthStore();

  // Form State
  const [center, setCenter] = useState<[number, number]>([40.4406, -79.9959]);
  const [radius, setRadius] = useState(5000);
  const [address, setAddress] = useState('');
  const [nodeCount, setNodeCount] = useState(mode === 'archipelago' ? 50 : 25);

  // UI State
  const [isGenerating, setIsGenerating] = useState(false);
  const [progress, setProgress] = useState(0);
  const [status, setStatus] = useState('');
  const [errorMsg, setErrorMsg] = useState('');

  const handleMapClick = (lat: number, lng: number) => {
    setCenter([lat, lng]);
  };

  const handleGenerate = async () => {
    setIsGenerating(true);
    setErrorMsg('');
    setStatus('Creating game session placeholder...');
    setProgress(10);

    try {
      const token = pb.authStore.token;
      
      // 1. Create DB Session
      const payload: any = {
        user: user?.id ?? '',
        status: 'SetupInProgress',
        radius: radius // the selected radius allows the DB save to succeed cleanly now!
      };
      
      if (mode === 'archipelago') {
        payload.ap_server_url = serverUrl;
        payload.ap_slot_name = slotName;
      }

      const createRes = await fetch('/api/sessions', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        },
        body: JSON.stringify(payload)
      });

      if (!createRes.ok) {
        const err = await createRes.json().catch(() => ({}));
        throw new Error(err.message ?? `Session creation failed: ${createRes.status}`);
      }

      const session = await createRes.json();
      const newSessionId = session.id;

      setProgress(40);
      setStatus('Generating intersection nodes...');

      // 2. Generate Nodes
      const genRes = await fetch(`/api/sessions/${newSessionId}/generate`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        },
        body: JSON.stringify({
          centerLat: center[0],
          centerLon: center[1],
          radius,
          nodeCount: mode === 'singleplayer' ? nodeCount : undefined,
          mode
        })
      });

      if (!genRes.ok) {
        const err = await genRes.json().catch(() => ({}));
        throw new Error(err.message ?? `Generation failed: ${genRes.status}`);
      }

      setProgress(100);
      setStatus('Success! Re-routing...');
      
      setTimeout(() => navigate(`/game/${newSessionId}`), 500);
    } catch (err: any) {
      setErrorMsg(err.message ?? 'Generation process failed.');
      setIsGenerating(false);
      setProgress(0);
    }
  };


  return (
    <div className="py-8 space-y-8">
      {/* Header */}
      <header className="max-w-4xl mx-auto text-center">
        <h1 className="text-3xl font-black text-white mb-2">Configure Your Session</h1>
        <p className="text-neutral-400">
          Search for a location or click on the map to set your starting point.
        </p>
      </header>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 items-start">
        {/* Sidebar Controls */}
        <div className="space-y-6 lg:order-1 order-2">
          <div className="bg-neutral-900 border border-neutral-800 rounded-2xl p-6 space-y-6">
            {/* Search */}
            <div className="space-y-2">
              <label className="text-xs font-black uppercase tracking-widest text-neutral-500">Location Search</label>
              <div className="flex gap-2">
                <div className="relative flex-1">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-neutral-500" />
                  <input
                    type="text"
                    value={address}
                    onChange={(e) => setAddress(e.target.value)}
                    placeholder="Pittsburgh, PA"
                    className="w-full bg-neutral-800 border border-neutral-700 rounded-xl py-2 pl-10 pr-4 text-sm text-white focus:outline-none focus:ring-1 focus:ring-orange-500"
                  />
                </div>
                <button className="btn btn-neutral btn-sm h-10 rounded-xl">Search</button>
              </div>
              <button className="w-full btn btn-neutral btn-sm h-10 rounded-xl gap-2 text-xs">
                <Navigation className="w-3 h-3" />
                Use My Location
              </button>
            </div>

            {/* Radius */}
            <div className="space-y-4 pt-4 border-t border-neutral-800">
              <div className="flex justify-between items-end">
                <label className="text-xs font-black uppercase tracking-widest text-neutral-500">Radius</label>
                <span className="text-orange-500 font-bold text-sm">{(radius / 1000).toFixed(1)} km</span>
              </div>
              <input
                type="range"
                min="500"
                max="20000"
                step="500"
                value={radius}
                onChange={(e) => setRadius(Number(e.target.value))}
                className="range range-xs range-primary"
              />
            </div>

            {/* Node Count */}
            <div className="space-y-4 pt-4 border-t border-neutral-800">
              <div className="flex justify-between items-end">
                <label className="text-xs font-black uppercase tracking-widest text-neutral-500">Intersections</label>
                <span className="text-white font-bold text-sm">{nodeCount}</span>
              </div>
              {mode === 'singleplayer' ? (
                <input
                  type="range"
                  min="10"
                  max="200"
                  step="5"
                  value={nodeCount}
                  onChange={(e) => setNodeCount(Number(e.target.value))}
                  className="range range-xs"
                />
              ) : (
                <div className="flex items-center gap-2 p-3 bg-neutral-800 rounded-xl">
                  <AlertCircle className="w-4 h-4 text-orange-500" />
                  <span className="text-xs text-neutral-400">Fixed from Archipelago seed</span>
                </div>
              )}
            </div>

            {/* Progress */}
            {isGenerating && (
              <div className="space-y-2 pt-4 border-t border-neutral-800 animate-in fade-in duration-300">
                <div className="flex justify-between text-xs mb-1">
                  <span className="text-orange-500 font-bold animate-pulse">{status}</span>
                  <span className="text-neutral-500">{Math.round(progress)}%</span>
                </div>
                <progress className="progress progress-primary w-full h-2" value={progress} max="100"></progress>
              </div>
            )}

            {/* Start Button */}
            <button
              onClick={handleGenerate}
              disabled={isGenerating}
              className="w-full btn btn-orange btn-lg h-14 rounded-2xl gap-3 font-black uppercase tracking-widest text-xs"
            >
              {isGenerating ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Generating...
                </>
              ) : (
                <>
                  <Play className="w-4 h-4 fill-white" />
                  Create Session
                </>
              )}
            </button>

            {errorMsg && (
              <div className="p-3 bg-red-500/10 border border-red-500/20 rounded-xl text-red-400 text-xs font-medium">
                {errorMsg}
              </div>
            )}
          </div>
        </div>

        {/* Map Display */}
        <div className="lg:col-span-2 lg:order-2 order-1">
          <div className="bg-neutral-900 border border-neutral-800 rounded-3xl overflow-hidden h-[600px] shadow-2xl relative">
            <MapContainer
              center={center}
              zoom={13}
              style={{ height: '100%', width: '100%', zIndex: 0 }}
            >
              <TileLayer
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                maxZoom={19}
              />
              <MapResizer />
              <MapEvents onMapClick={handleMapClick} />
              <Marker position={center} icon={DefaultIcon} />
              <Circle
                center={center}
                radius={radius}
                pathOptions={{
                  color: '#f97316',
                  fillColor: '#f97316',
                  fillOpacity: 0.1,
                  weight: 2
                }}
              />
            </MapContainer>
            
            {/* Overlay Info */}
            <div className="absolute top-4 left-4 z-[400] bg-neutral-900/90 backdrop-blur-md border border-neutral-800 px-4 py-2 rounded-xl text-xs font-bold text-neutral-300 shadow-xl">
              Mode: {mode.toUpperCase()}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default SessionSetup;
