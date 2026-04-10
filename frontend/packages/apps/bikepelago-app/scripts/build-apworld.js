import AdmZip from 'adm-zip';
import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const sourceDir = path.join(__dirname, '../apworld/bikeapelago');
const publicDir = path.join(__dirname, '../public');
const outputFile = path.join(publicDir, 'bikeapelago.apworld');

// Ensure public directory exists
if (!fs.existsSync(publicDir)) {
  fs.mkdirSync(publicDir, { recursive: true });
}

const zip = new AdmZip();
// Add the module folder as 'bikeapelago/' inside the zip
zip.addLocalFolder(sourceDir, 'bikeapelago');
// Add archipelago.json at zip root (required by Archipelago to register the world)
const metaFile = path.join(__dirname, '../apworld/archipelago.json');
if (fs.existsSync(metaFile)) {
  zip.addLocalFile(metaFile);
}
zip.writeZip(outputFile);

console.log(`Generated APWorld file at ${outputFile}`);
