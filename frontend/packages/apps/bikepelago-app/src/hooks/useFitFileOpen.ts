import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Capacitor } from '@capacitor/core';
import { App } from '@capacitor/app';

import { useFitImportStore } from '../store/fitImportStore';

// Listens for iOS/Android file-open events (Share Sheet, Files app) and converts
// the incoming file:// URL into a File object, then routes to the import page.
export function useFitFileOpen() {
  const setPendingFile = useFitImportStore((s) => s.setPendingFile);
  const navigate = useNavigate();

  useEffect(() => {
    if (!Capacitor.isNativePlatform()) return;

    const listenerPromise = App.addListener('appUrlOpen', (event) => {
      if (!event.url.toLowerCase().endsWith('.fit')) return;

      void (async () => {
        try {
          // convertFileSrc maps file:// to capacitor://localhost/_capacitor_file_/...
          // which WKWebView can fetch without cross-origin restrictions.
          const webUrl = Capacitor.convertFileSrc(event.url);
          const response = await fetch(webUrl);
          if (!response.ok) throw new Error(`HTTP ${response.status}`);
          const buffer = await response.arrayBuffer();
          const filename = event.url.split('/').pop() ?? 'activity.fit';
          setPendingFile(
            new File([buffer], filename, { type: 'application/octet-stream' }),
          );
          void navigate('/fit-import');
        } catch (err) {
          console.error('[useFitFileOpen] Failed to read FIT file:', err);
        }
      })();
    });

    return () => {
      void listenerPromise.then((h) => h.remove());
    };
  }, [setPendingFile, navigate]);
}
