import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.bikeapelago.app',
  appName: 'Bikeapelago',
  webDir: 'dist',
  server: {
    // Use https scheme so iOS WKWebView allows mixed-content-free API calls
    androidScheme: 'https',
  },
  plugins: {
    Geolocation: {
      // iOS NSLocationWhenInUseUsageDescription is set via the Xcode project plist.
      // This placeholder is here as a reminder — update it in ios/App/App/Info.plist.
    },
  },
};

export default config;
