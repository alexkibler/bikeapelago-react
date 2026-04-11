import fs from 'fs';
import FitParser from 'fit-file-parser';
import { SportsLib } from '@sports-alliance/sports-lib';

async function test() {
  const filePath = '/Users/alex/Downloads/Bikeapelago Route.fit';
  if (!fs.existsSync(filePath)) {
    console.error("File not found:", filePath);
    return;
  }
  const buffer = fs.readFileSync(filePath);
  
  console.log("--- fit-file-parser ---");
  const fitParser = new FitParser({ force: true, speedUnit: 'm/s', lengthUnit: 'm', mode: 'both' });
  fitParser.parse(buffer, (err, data) => {
    if (err) console.error("fit-file-parser error:", err);
    else {
      console.log("Records length:", data.records ? data.records.length : 0);
      if (data.records && data.records.length > 0) {
        console.log("First record:", JSON.stringify(data.records[0]));
      }
    }
  });
}

test();
