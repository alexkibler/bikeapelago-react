import { useEffect, useState } from 'react';

import L from 'leaflet';
// Fix Leaflet marker icon issue
import markerIcon from 'leaflet/dist/images/marker-icon.png';
import markerShadow from 'leaflet/dist/images/marker-shadow.png';
import 'leaflet/dist/leaflet.css';
import {
  AlertCircle,
  Bike,
  Footprints,
  HelpCircle,
  Loader2,
  Navigation,
  Play,
  Search,
} from 'lucide-react';
import {
  Circle,
  MapContainer,
  Marker,
  TileLayer,
  useMap,
  useMapEvents,
} from 'react-leaflet';
import { useNavigate, useSearchParams } from 'react-router-dom';

import { getToken, useAuthStore } from '../store/authStore';

const DefaultIcon = L.icon({
  iconUrl: markerIcon,
  shadowUrl: markerShadow,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
});
L.Marker.prototype.options.icon = DefaultIcon;

const MapEvents = ({
  onMapClick,
}: {
  onMapClick: (lat: number, lng: number) => void;
}) => {
  useMapEvents({
    click(e) {
      onMapClick(e.latlng.lat, e.latlng.lng);
    },
  });
  return null;
};

const MapCenterUpdater = ({ center }: { center: [number, number] }) => {
  const map = useMap();
  useEffect(() => {
    map.setView(center, map.getZoom());
  }, [center, map]);
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
  const sessionName = searchParams.get('name') || '';
  const mode = searchParams.get('mode') || 'archipelago';

  const { user } = useAuthStore();

  // Form State
  const [center, setCenter] = useState<[number, number]>([40.4406, -79.9959]);
  const [radius, setRadius] = useState(5000);
  const [address, setAddress] = useState('');
  const [nodeCount, setNodeCount] = useState(mode === 'archipelago' ? 50 : 25);
  const [travelMode, setTravelMode] = useState<'bike' | 'walk'>('bike');

  // UI State
  const [isGenerating, setIsGenerating] = useState(false);
  const [isLocating, setIsLocating] = useState(false);
  const [progress, setProgress] = useState(0);
  const [status, setStatus] = useState('');
  const [errorMsg, setErrorMsg] = useState('');

  const handleUseMyLocation = () => {
    if (!navigator.geolocation) {
      setErrorMsg('Geolocation is not supported');
      return;
    }

    setIsLocating(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setCenter([pos.coords.latitude, pos.coords.longitude]);
        setIsLocating(false);
      },
      (err) => {
        setErrorMsg(`Location error: ${err.message}`);
        setIsLocating(false);
      },
      { enableHighAccuracy: true },
    );
  };

  const handleMapClick = (lat: number, lng: number) => {
    setCenter([lat, lng]);
  };

  const handleGenerate = async () => {
    setIsGenerating(true);
    setErrorMsg('');
    setStatus('Creating game session placeholder...');
    setProgress(10);

    try {
      const token = getToken();

      // 1. Create DB Session
      const payload: {
        user: string;
        name: string;
        status: string;
        radius: number;
        center_lat: number;
        center_lon: number;
        ap_server_url?: string;
        ap_slot_name?: string;
        mode: string;
      } = {
        user: user?.id ?? '',
        name: sessionName,
        status: 'SetupInProgress',
        radius: radius, // the selected radius allows the DB save to succeed cleanly now!
        center_lat: center[0],
        center_lon: center[1],
        mode: mode,
      };

      if (mode === 'archipelago') {
        payload.ap_server_url = serverUrl;
        payload.ap_slot_name = slotName;
      }

      const createRes = await fetch('/api/sessions', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify(payload),
      });

      if (!createRes.ok) {
        const err = (await createRes.json().catch(() => ({}))) as {
          message?: string;
        };
        throw new Error(
          err.message ?? `Session creation failed: ${createRes.status}`,
        );
      }

      const session = (await createRes.json()) as { id: string };
      const newSessionId = session.id;

      setProgress(40);
      setStatus('Generating intersection nodes...');

      // 2. Generate Nodes
      const genRes = await fetch(`/api/sessions/${newSessionId}/generate`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({
          centerLat: center[0],
          centerLon: center[1],
          radius,
          nodeCount: mode === 'singleplayer' ? nodeCount : undefined,
          gameMode: mode,
          mode: travelMode,
        }),
      });

      if (!genRes.ok) {
        const err = (await genRes.json().catch(() => ({}))) as {
          message?: string;
        };
        throw new Error(err.message ?? `Generation failed: ${genRes.status}`);
      }

      setProgress(100);
      setStatus('Success! Re-routing...');

      setTimeout(() => navigate(`/game/${newSessionId}`), 500);
    } catch (err: unknown) {
      setErrorMsg(
        err instanceof Error ? err.message : 'Generation process failed.',
      );
      setIsGenerating(false);
      setProgress(0);
    }
  };

  return (
    <div className='py-8 space-y-8 max-w-screen-xl mx-auto px-6'>
      {/* Header */}
      <header className='max-w-4xl mx-auto text-center'>
        <h1 className='text-3xl font-black text-[var(--color-text-hex)] mb-2'>
          Configure Your Session
        </h1>
        <p className='text-[var(--color-text-muted-hex)]'>
          Search for a location or click on the map to set your starting point.
        </p>
      </header>

      <div className='grid grid-cols-1 lg:grid-cols-3 gap-8 items-start'>
        {/* Sidebar Controls */}
        <div className='space-y-6 lg:order-1 order-2'>
          <div className='bg-[var(--color-surface-hex)] border border-[var(--color-border-hex)] rounded-2xl p-6 space-y-6'>
            {/* Search */}
            <div className='space-y-2'>
              <label className='text-xs font-black uppercase tracking-widest text-[var(--color-text-subtle-hex)]'>
                Location Search
              </label>
              <div className='flex gap-2'>
                <div className='relative flex-1'>
                  <Search className='absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-subtle-hex)]' />
                  <input
                    type='text'
                    value={address}
                    onChange={(e) => setAddress(e.target.value)}
                    placeholder='Pittsburgh, PA'
                    className='w-full bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] rounded-xl py-2 pl-10 pr-4 text-sm text-[var(--color-text-hex)] focus:outline-none focus:ring-1 focus:ring-[var(--color-primary-hex)]'
                  />
                </div>
                <button className='btn btn-neutral btn-sm h-10 rounded-xl'>
                  Search
                </button>
              </div>
              <button
                onClick={handleUseMyLocation}
                disabled={isLocating}
                className='w-full btn btn-neutral btn-sm h-10 rounded-xl gap-2 text-xs'
              >
                {isLocating ? (
                  <Loader2 className='w-3 h-3 animate-spin' />
                ) : (
                  <Navigation className='w-3 h-3' />
                )}
                {isLocating ? 'Locating...' : 'Use My Location'}
              </button>
            </div>

            {/* Radius */}
            <div className='space-y-4 pt-4 border-t border-[var(--color-border-hex)]'>
              <div className='flex justify-between items-end'>
                <label className='text-xs font-black uppercase tracking-widest text-[var(--color-text-subtle-hex)]'>
                  Radius
                </label>
                <span className='text-[var(--color-primary-hex)] font-bold text-sm'>
                  {(radius / 1000).toFixed(1)} km
                </span>
              </div>
              <input
                type='range'
                min='500'
                max='20000'
                step='500'
                value={radius}
                onChange={(e) => setRadius(Number(e.target.value))}
                className='range range-xs range-primary'
              />
            </div>

            {/* Node Count */}
            <div className='space-y-4 pt-4 border-t border-[var(--color-border-hex)]'>
              <div className='flex justify-between items-end'>
                <label className='text-xs font-black uppercase tracking-widest text-[var(--color-text-subtle-hex)]'>
                  Intersections
                </label>
                <span className='text-[var(--color-text-hex)] font-bold text-sm'>
                  {nodeCount}
                </span>
              </div>
              {mode === 'singleplayer' ? (
                <input
                  type='range'
                  min='10'
                  max='200'
                  step='5'
                  value={nodeCount}
                  onChange={(e) => setNodeCount(Number(e.target.value))}
                  className='range range-xs'
                />
              ) : (
                <div className='flex items-center gap-2 p-3 bg-[var(--color-surface-alt-hex)] rounded-xl'>
                  <AlertCircle className='w-4 h-4 text-[var(--color-primary-hex)]' />
                  <span className='text-xs text-[var(--color-text-muted-hex)]'>
                    Fixed from Archipelago seed
                  </span>
                </div>
              )}
            </div>

            {/* Travel Mode */}
            <div className='space-y-4 pt-4 border-t border-[var(--color-border-hex)]'>
              <div className='flex justify-between items-center'>
                <div className='flex items-center gap-2'>
                  <label className='text-xs font-black uppercase tracking-widest text-[var(--color-text-subtle-hex)]'>
                    Travel Mode
                  </label>
                  <div className='group relative'>
                    <HelpCircle className='w-3.5 h-3.5 text-[var(--color-text-subtle-hex)] cursor-help hover:text-[var(--color-primary-hex)] transition-colors' />
                    <div className='absolute left-1/2 -translate-x-1/2 bottom-full mb-3 w-64 p-4 bg-neutral-900/95 backdrop-blur-md border border-neutral-800 rounded-2xl shadow-2xl opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all duration-300 z-50 pointer-events-none'>
                      <h4 className='font-bold text-xs text-orange-500 mb-2 uppercase tracking-tight'>
                        How modes differ
                      </h4>
                      <div className='space-y-3'>
                        <div>
                          <p className='text-[10px] font-black text-neutral-300 mb-1'>
                            BIKE MODE
                          </p>
                          <p className='text-[11px] text-neutral-400 leading-relaxed'>
                            Prioritizes cycleways, residential streets, and
                            paved tracks. Avoids major highways and unsuitable
                            surfaces.
                          </p>
                        </div>
                        <div>
                          <p className='text-[10px] font-black text-neutral-300 mb-1'>
                            WALK MODE
                          </p>
                          <p className='text-[11px] text-neutral-400 leading-relaxed'>
                            Includes footpaths, pedestrian zones, steps, and
                            trails that might be inaccessible to bicycles.
                          </p>
                        </div>
                      </div>
                      <div className='absolute bottom-[-6px] left-1/2 -translate-x-1/2 w-3 h-3 bg-neutral-900 border-b border-r border-neutral-800 rotate-45'></div>
                    </div>
                  </div>
                </div>
                <span className='text-[var(--color-text-hex)] font-bold text-sm uppercase'>
                  {travelMode}
                </span>
              </div>
              <div className='grid grid-cols-2 gap-2'>
                <button
                  type='button'
                  onClick={() => setTravelMode('bike')}
                  className={`flex items-center justify-center gap-2 p-3 rounded-xl border-2 transition-all ${
                    travelMode === 'bike'
                      ? 'border-orange-500 bg-orange-500/5 text-orange-500'
                      : 'border-[var(--color-border-hex)] bg-[var(--color-surface-alt-hex)] text-[var(--color-text-muted-hex)] hover:border-[var(--color-border-strong-hex)]'
                  }`}
                >
                  <Bike className='w-4 h-4' />
                  <span className='font-bold text-xs'>Bike</span>
                </button>
                <button
                  type='button'
                  onClick={() => setTravelMode('walk')}
                  className={`flex items-center justify-center gap-2 p-3 rounded-xl border-2 transition-all ${
                    travelMode === 'walk'
                      ? 'border-orange-500 bg-orange-500/5 text-orange-500'
                      : 'border-[var(--color-border-hex)] bg-[var(--color-surface-alt-hex)] text-[var(--color-text-muted-hex)] hover:border-[var(--color-border-strong-hex)]'
                  }`}
                >
                  <Footprints className='w-4 h-4' />
                  <span className='font-bold text-xs'>Walk</span>
                </button>
              </div>
            </div>

            {/* Progress */}
            {isGenerating && (
              <div className='space-y-2 pt-4 border-t border-[var(--color-border-hex)] animate-in fade-in duration-300'>
                <div className='flex justify-between text-xs mb-1'>
                  <span className='text-[var(--color-primary-hex)] font-bold animate-pulse'>
                    {status}
                  </span>
                  <span className='text-[var(--color-text-subtle-hex)]'>
                    {Math.round(progress)}%
                  </span>
                </div>
                <progress
                  className='progress progress-primary w-full h-2'
                  value={progress}
                  max='100'
                ></progress>
              </div>
            )}

            {/* Start Button */}
            <button
              onClick={handleGenerate}
              disabled={isGenerating}
              className='w-full btn btn-orange btn-lg h-14 rounded-2xl gap-3 font-black uppercase tracking-widest text-xs'
            >
              {isGenerating ? (
                <>
                  <Loader2 className='w-4 h-4 animate-spin' />
                  Generating...
                </>
              ) : (
                <>
                  <Play className='w-4 h-4 fill-white' />
                  Create Session
                </>
              )}
            </button>

            {errorMsg && (
              <div className='p-3 bg-[var(--color-error-hex)]/10 border border-[var(--color-error-hex)]/20 rounded-xl text-[var(--color-error-hex)] text-xs font-medium'>
                {errorMsg}
              </div>
            )}
          </div>
        </div>

        {/* Map Display */}
        <div className='lg:col-span-2 lg:order-2 order-1'>
          <div className='bg-[var(--color-surface-hex)] border border-[var(--color-border-hex)] rounded-3xl overflow-hidden h-[600px] shadow-2xl relative'>
            <MapContainer
              center={center}
              zoom={13}
              style={{ height: '100%', width: '100%', zIndex: 0 }}
            >
              <TileLayer
                url='https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png'
                attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                maxZoom={19}
              />
              <MapResizer />
              <MapEvents onMapClick={handleMapClick} />
              <MapCenterUpdater center={center} />
              <Marker position={center} icon={DefaultIcon} />
              <Circle
                center={center}
                radius={radius}
                pathOptions={{
                  color: '#f97316',
                  fillColor: '#f97316',
                  fillOpacity: 0.1,
                  weight: 2,
                }}
              />
            </MapContainer>

            {/* Overlay Info */}
            <div className='absolute top-4 left-4 z-[400] bg-neutral-900/90 backdrop-blur-md border border-neutral-800 px-4 py-2 rounded-xl text-xs font-bold text-neutral-300 shadow-xl'>
              Mode: {mode.toUpperCase()}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default SessionSetup;
