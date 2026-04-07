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
// We add it to 'bikeapelago' so the zip root has the module folder
zip.addLocalFolder(sourceDir, 'bikeapelago');
zip.writeZip(outputFile);

console.log(`Generated APWorld file at ${outputFile}`);
