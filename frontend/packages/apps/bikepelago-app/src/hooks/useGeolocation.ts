import { useEffect, useRef } from 'react';

import { useGameStore } from '../store/gameStore';

export function useGeolocation() {
  const setUserLocation = useGameStore((state) => state.setUserLocation);
  const watchId = useRef<number | null>(null);

  useEffect(() => {
    if (!navigator.geolocation) {
      console.error('Geolocation is not supported by your browser');
      return;
    }

    const handleSuccess = (position: GeolocationPosition) => {
      const { latitude, longitude } = position.coords;
      console.log(`Updated user location: ${latitude}, ${longitude}`);
      setUserLocation([latitude, longitude]);
    };

    const handleError = (error: GeolocationPositionError) => {
      console.error('Geolocation error:', error.message);
    };

    // Options for high accuracy (essential for biking)
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

    return () => {
      if (watchId.current !== null) {
        navigator.geolocation.clearWatch(watchId.current);
      }
    };
  }, [setUserLocation]);

  return null; // This hook just manages global state
}
