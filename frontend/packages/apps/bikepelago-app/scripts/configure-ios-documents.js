#!/usr/bin/env node
// Patches ios/App/App/Info.plist to declare the .fit UTI and register Bikeapelago
// as a handler for Garmin FIT activity files.
//
// Run once after `npx cap add ios`:
//   node scripts/configure-ios-documents.js

import { readFileSync, writeFileSync, existsSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const plistPath = resolve(__dirname, '../ios/App/App/Info.plist');

if (!existsSync(plistPath)) {
  console.error(
    'Info.plist not found at', plistPath,
    '\nRun `npx cap add ios` first, then re-run this script.',
  );
  process.exit(1);
}

let content = readFileSync(plistPath, 'utf8');

if (content.includes('com.garmin.fit')) {
  console.log('Info.plist already contains FIT file type configuration. Nothing to do.');
  process.exit(0);
}

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

// Insert before the root closing </dict>
content = content.replace(/(<\/dict>\s*<\/plist>)/, `${fitEntries}\n$1`);

writeFileSync(plistPath, content, 'utf8');
console.log('Patched Info.plist with FIT file type association.');
console.log('Run `npx cap sync` to apply the changes to the Xcode project.');
