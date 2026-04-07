import { FitWriter } from '@markw65/fit-file-writer';
import fs from 'fs';

const toSemicircles = (deg: number) => Math.round(deg * (Math.pow(2, 31) / 180));
const writer = new FitWriter();
writer.writeMessage('file_id', { type: 'activity', manufacturer: 'development', product: 0, serial_number: 123, time_created: writer.time(new Date()) });
writer.writeMessage('activity', { timestamp: writer.time(new Date()), num_sessions: 1, type: 'manual', event: 'activity', event_type: 'start' });
writer.writeMessage('record', { timestamp: writer.time(new Date()), position_lat: toSemicircles(40.7128), position_long: toSemicircles(-74.006), altitude: 250, distance: 100 });
const fitData = writer.finish();
fs.writeFileSync('proof.fit', Buffer.from(fitData.buffer, fitData.byteOffset, fitData.byteLength));
