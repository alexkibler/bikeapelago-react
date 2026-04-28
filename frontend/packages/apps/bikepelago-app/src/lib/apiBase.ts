const normalizeApiOrigin = (value: string | undefined): string => {
  const trimmed = value?.trim() ?? '';
  return trimmed.replace(/\/+$/, '');
};

export const API_ORIGIN = normalizeApiOrigin(
  import.meta.env.VITE_PUBLIC_API_URL as string | undefined,
);

export const API_BASE = API_ORIGIN ? `${API_ORIGIN}/api` : '/api';
export const HUBS_BASE = API_ORIGIN ? `${API_ORIGIN}/hubs` : '/hubs';
