import { useEffect, useRef } from 'react';
import { Capacitor } from '@capacitor/core';
import { Geolocation } from '@capacitor/geolocation';

import { useGameStore } from '../store/gameStore';

export function useGeolocation() {
  const setUserLocation = useGameStore((state) => state.setUserLocation);
  const watchId = useRef<number | null>(null);

  useEffect(() => {
    if (!navigator.geolocation) {
      console.error('Geolocation is not supported by your browser');
      return;
    }

    let cancelled = false;

    const start = async () => {
      // On native platforms request permission explicitly before touching navigator.geolocation.
      // Without this iOS silently ignores watchPosition calls.
      if (Capacitor.isNativePlatform()) {
        const status = await Geolocation.requestPermissions();
        if (status.location !== 'granted' && status.coarseLocation !== 'granted') {
          console.error('Geolocation permission denied by user');
          return;
        }
      }

      if (cancelled) return;

      const handleSuccess = (position: GeolocationPosition) => {
        const { latitude, longitude } = position.coords;
        console.log(`Updated user location: ${latitude}, ${longitude}`);
        setUserLocation([latitude, longitude]);
      };

      const handleError = (error: GeolocationPositionError) => {
        console.error('Geolocation error:', error.message);
      };

      const options = {
        enableHighAccuracy: true,
        timeout: 10000,
        maximumAge: 0,
      };

      watchId.current = navigator.geolocation.watchPosition(
        handleSuccess,
        handleError,
        options,
      );
    };

    void start();

    return () => {
      cancelled = true;
      if (watchId.current !== null) {
        navigator.geolocation.clearWatch(watchId.current);
      }
    };
  }, [setUserLocation]);

  return null;
}
