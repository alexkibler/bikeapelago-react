import fs from 'fs';
import FitParser from 'fit-file-parser';

async function test() {
  const filePath = '/Users/alex/Downloads/Bikeapelago Route.fit';
  const nodeBuffer = fs.readFileSync(filePath);
  const u8 = new Uint8Array(nodeBuffer.buffer.slice(nodeBuffer.byteOffset, nodeBuffer.byteOffset + nodeBuffer.byteLength));
  
  const fitParser = new FitParser({ force: true, speedUnit: 'm/s', lengthUnit: 'm', mode: 'both' });
  try {
    const data = await new Promise((resolve, reject) => {
        fitParser.parse(u8, (err, data) => err ? reject(err) : resolve(data));
    });
    console.log("Records:", data.records ? data.records.length : "None");
  } catch (e) {
    console.error("Error:", e);
  }
}
test();
