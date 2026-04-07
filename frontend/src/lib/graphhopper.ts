// Simple mock config since GraphHopper is accessed via LRM inside Leaflet components.
// During the component porting this allows those components to use the VITE variable.

export const getGraphhopperUrl = () => {
    // If running inside Capacitor, we would want absolute proxy URL.
    // In dev mode, we rely on the vite proxy pointing to the .NET API.
    return import.meta.env.VITE_PUBLIC_API_URL
        ? `${import.meta.env.VITE_PUBLIC_API_URL}/api/gh`
        : '/api/gh';
};
