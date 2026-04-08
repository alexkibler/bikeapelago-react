import fs from 'fs';
import FitParser from 'fit-file-parser';

async function test() {
  const filePath = '/Users/alex/Downloads/Bikeapelago Route.fit';
  const nodeBuffer = fs.readFileSync(filePath);
  // simulate arrayBuffer from browser
  const arrayBuffer = nodeBuffer.buffer.slice(nodeBuffer.byteOffset, nodeBuffer.byteOffset + nodeBuffer.byteLength);
  
  const fitParser = new FitParser({ force: true, speedUnit: 'm/s', lengthUnit: 'm', mode: 'both' });
  fitParser.parse(arrayBuffer, (err, data) => {
    if (err) console.error("fit-file-parser error:", err);
    else {
      console.log("Records:", data.records.length);
    }
  });
}

test();
