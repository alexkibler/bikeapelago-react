#!/usr/bin/env node
// Patches ios/App/App/Info.plist to declare the .fit UTI and register Bikeapelago
// as a handler for Garmin FIT activity files, then runs pod install.
//
// Run once after `npx cap add ios`:
//   node scripts/configure-ios-documents.js

import { execSync } from 'child_process';
import { readFileSync, writeFileSync, existsSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const appRoot = resolve(__dirname, '..');
const plistPath = resolve(appRoot, 'ios/App/App/Info.plist');
const podfilePath = resolve(appRoot, 'ios/App/Podfile');

if (!existsSync(plistPath)) {
  console.error(
    'ios/ project not found. Run these first:\n' +
    '  npx cap add ios\n' +
    'Then re-run this script.',
  );
  process.exit(1);
}

// ── Patch Info.plist ────────────────────────────────────────────────────────

let content = readFileSync(plistPath, 'utf8');

if (content.includes('com.garmin.fit')) {
  console.log('Info.plist already contains FIT file type configuration. Skipping plist patch.');
} else {
  // FIT files have no standard Apple UTI — we import/declare a custom one so iOS
  // knows what .fit means and can route opens to this app.
  const fitEntries = `
\t<key>UTImportedTypeDeclarations</key>
\t<array>
\t\t<dict>
\t\t\t<key>UTTypeIdentifier</key>
\t\t\t<string>com.garmin.fit</string>
\t\t\t<key>UTTypeDescription</key>
\t\t\t<string>Garmin FIT Activity</string>
\t\t\t<key>UTTypeConformsTo</key>
\t\t\t<array>
\t\t\t\t<string>public.data</string>
\t\t\t</array>
\t\t\t<key>UTTypeTagSpecification</key>
\t\t\t<dict>
\t\t\t\t<key>public.filename-extension</key>
\t\t\t\t<array>
\t\t\t\t\t<string>fit</string>
\t\t\t\t</array>
\t\t\t</dict>
\t\t</dict>
\t</array>
\t<key>CFBundleDocumentTypes</key>
\t<array>
\t\t<dict>
\t\t\t<key>CFBundleTypeName</key>
\t\t\t<string>Garmin FIT Activity</string>
\t\t\t<key>LSHandlerRank</key>
\t\t\t<string>Alternate</string>
\t\t\t<key>LSItemContentTypes</key>
\t\t\t<array>
\t\t\t\t<string>com.garmin.fit</string>
\t\t\t</array>
\t\t\t<key>CFBundleTypeRole</key>
\t\t\t<string>Viewer</string>
\t\t</dict>
\t</array>`;

  content = content.replace(/(<\/dict>\s*<\/plist>)/, `${fitEntries}\n$1`);
  writeFileSync(plistPath, content, 'utf8');
  console.log('✓ Patched Info.plist with FIT file type association.');
}

// ── Also add NSLocationWhenInUseUsageDescription if missing ─────────────────

content = readFileSync(plistPath, 'utf8');
if (!content.includes('NSLocationWhenInUseUsageDescription')) {
  const locationKey = `
\t<key>NSLocationWhenInUseUsageDescription</key>
\t<string>Bikeapelago needs your location to track your ride and show nearby checkpoints.</string>`;
  content = content.replace(/(<\/dict>\s*<\/plist>)/, `${locationKey}\n$1`);
  writeFileSync(plistPath, content, 'utf8');
  console.log('✓ Added NSLocationWhenInUseUsageDescription to Info.plist.');
}

// ── Run pod install ─────────────────────────────────────────────────────────
// Required so Xcode can resolve the Capacitor framework modules.
// "No such module 'Capacitor'" in Xcode means this step was skipped.

if (!existsSync(podfilePath)) {
  console.error('Podfile not found — the iOS project may be incomplete. Try `npx cap add ios` again.');
  process.exit(1);
}

console.log('\nRunning pod install (this may take a minute on first run)…');
try {
  execSync('pod install', {
    cwd: resolve(appRoot, 'ios/App'),
    stdio: 'inherit',
  });
  console.log('✓ pod install complete.');
} catch {
  console.error(
    '\npod install failed. Make sure CocoaPods is installed:\n' +
    '  sudo gem install cocoapods\n' +
    'Then re-run this script.',
  );
  process.exit(1);
}

console.log(
  '\nSetup complete. Open the project in Xcode with:\n' +
  '  npx cap open ios\n' +
  '\nIMPORTANT: Xcode must open App.xcworkspace (not App.xcodeproj).\n' +
  '`cap open ios` does this automatically.',
);
