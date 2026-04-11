import fs from 'fs';
import FitParser from 'fit-file-parser';

async function test() {
  const filePath = '/Users/alex/Downloads/Bikeapelago Route.fit';
  const nodeBuffer = fs.readFileSync(filePath);
  const arrayBuffer = nodeBuffer.buffer.slice(nodeBuffer.byteOffset, nodeBuffer.byteOffset + nodeBuffer.byteLength);
  
  const fitParser = new FitParser({ force: true, speedUnit: 'm/s', lengthUnit: 'm', mode: 'both' });
  try {
    const data = await new Promise((resolve, reject) => {
        fitParser.parse(arrayBuffer, (err, data) => err ? reject(err) : resolve(data));
    });
    console.log("Records:", data.records.length);
  } catch (e) {
    console.error("Error:", e);
  }
}

test();
