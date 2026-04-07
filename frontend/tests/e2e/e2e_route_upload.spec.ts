import { test, expect } from '@playwright/test';
import { FitWriter } from '@markw65/fit-file-writer';
import * as fs from 'fs';
import * as path from 'path';

test.describe('E2E Map Routing, GPX Download, FIT Conversion, and Upload', () => {
	test.setTimeout(180000); // 3 minutes for full flow

	test.beforeEach(async ({ page }) => {
		page.on('console', (msg) => {
			if (msg.type() === 'error') console.error(`[Browser Error] ${msg.text()}`);
		});
		
		// We DON'T set PLAYWRIGHT_TEST = true here because we want to test the real analysis
		// with the mocked Overpass backend.
	});

	function createFitFileFromGpx(gpxPath: string, fitPath: string) {
		const gpxContent = fs.readFileSync(gpxPath, 'utf8');
		const regex = /<trkpt lat="([^"]+)" lon="([^"]+)">/g;
		const points: {lat: number, lon: number}[] = [];
		let match;
		while ((match = regex.exec(gpxContent)) !== null) {
			points.push({
				lat: parseFloat(match[1]),
				lon: parseFloat(match[2])
			});
		}

		if (points.length === 0) {
			throw new Error('No track points found in GPX file');
		}

		const toSemicircles = (deg: number) => Math.round(deg * (Math.pow(2, 31) / 180));
		const writer = new FitWriter();
		writer.writeMessage('file_id', {
			type: 'activity',
			manufacturer: 'development',
			product: 0,
			serial_number: 123,
			time_created: writer.time(new Date())
		});
		
		const startTime = new Date();
		writer.writeMessage('activity', {
			timestamp: writer.time(startTime),
			num_sessions: 1,
			type: 'manual',
			event: 'activity',
			event_type: 'start'
		});
		writer.writeMessage('event', {
			timestamp: writer.time(startTime),
			event: 'timer',
			event_type: 'start',
			event_group: 0
		});
		writer.writeMessage('session', {
			timestamp: writer.time(startTime),
			start_time: writer.time(startTime),
			sport: 'cycling',
			total_elapsed_time: points.length * 10,
			total_timer_time: points.length * 10,
			total_distance: 100 * points.length,
			total_ascent: 50
		});
		writer.writeMessage('lap', {
			timestamp: writer.time(startTime),
			start_time: writer.time(startTime),
			total_elapsed_time: points.length * 10,
			total_timer_time: points.length * 10,
			total_distance: 100 * points.length,
			total_ascent: 50
		});

		points.forEach((pt, i) => {
			writer.writeMessage('record', {
				timestamp: writer.time(new Date(startTime.getTime() + i * 1000)),
				position_lat: toSemicircles(pt.lat),
				position_long: toSemicircles(pt.lon),
				altitude: 250,
				distance: i * 10,
				heart_rate: 140,
				power: 200
			});
		});

		const fitData = writer.finish();
		fs.writeFileSync(fitPath, Buffer.from(fitData.buffer, fitData.byteOffset, fitData.byteLength));
	}

	test('complete routing to upload workflow', async ({ page }) => {
		// 1. Create New Session
		console.log('Starting new game...');
		await page.goto('/new-game');
		await page.click('button:has-text("Single Player")');
		await page.click('button:has-text("Start Single Player")');

		await page.waitForURL(/\/setup-session/);
		console.log('Generating session nodes...');
		await page.click('button:has-text("Create Session")');

		await page.waitForURL(/\/game\//, { timeout: 60000 });
		const sessionId = page.url().split('/').pop();
		console.log(`Created session: ${sessionId}`);

		// 2. Open Route Builder and wait for nodes
		// Enable Mock Mode for routing to avoid external backend dependency
		await page.evaluate(() => { (window as any).PLAYWRIGHT_TEST = true; });

		console.log('Opening Route Builder tab...');
		await page.locator('button:has-text("Route Builder")').filter({ visible: true }).click();
		await expect(page.locator('h2:has-text("Route Builder")')).toBeVisible({ timeout: 15000 });

		// Wait for nodes to load in the list
		const nodeItem = page.locator('button >> text=Mock Node').first();
		await expect(nodeItem).toBeVisible({ timeout: 15000 });

		// 3. Click two nodes to start route
		console.log('Adding nodes to route...');
		await page.locator('button >> text=Mock Node').nth(0).click();
		await page.locator('button >> text=Mock Node').nth(1).click();

		// 4. Download GPX
		console.log('Waiting for Download GPX button...');
		const downloadBtn = page.locator('button:has-text("Download GPX"), button:has-text("GPX")').first();
		await expect(downloadBtn).toBeEnabled({ timeout: 15000 });

		console.log('Downloading GPX...');
		const downloadPromise = page.waitForEvent('download');
		await downloadBtn.click();
		const download = await downloadPromise;
		
		const gpxPath = path.join(process.cwd(), 'temp_route.gpx');
		await download.saveAs(gpxPath);
		expect(fs.existsSync(gpxPath)).toBeTruthy();

		// 5. Convert GPX to FIT
		console.log('Converting GPX to FIT...');
		const fitPath = path.join(process.cwd(), 'temp_route.fit');
		createFitFileFromGpx(gpxPath, fitPath);
		expect(fs.existsSync(fitPath)).toBeTruthy();
		console.log('FIT file created successfully.');

		// Disable Mock Mode for upload to test real analysis logic against generated nodes
		await page.evaluate(() => { (window as any).PLAYWRIGHT_TEST = false; });

		// 6. Upload and Analyze
		console.log('Switching to Upload tab...');
		await page.locator('button:has-text("Upload")').filter({ visible: true }).click();
		await expect(page.locator('.panel-title')).toContainText('Upload', { timeout: 15000 });

		console.log('Uploading FIT file...');
		await page.locator('input#file-upload').setInputFiles(fitPath);

		console.log('Analyzing ride...');
		const analyzeBtn = page.locator('button:has-text("Analyze Ride")');
		await expect(analyzeBtn).toBeVisible();
		await analyzeBtn.click();

		// 7. Verify analysis reached at least one node
		console.log('Verifying analysis...');
		await expect(page.locator('text=DISTANCE')).toBeVisible({ timeout: 30000 });
		
		// If real analysis is working, we should see green "Location X" boxes
		const reachedLocation = page.locator('text=Location');
		await expect(reachedLocation.first()).toBeVisible({ timeout: 15000 });
		console.log('Ride validated with real analysis!');

		// 8. Confirm
		await page.click('button:has-text("Confirm & Send")');
		await expect(page.locator('text=Successfully validated')).toBeVisible({ timeout: 15000 });

		// Cleanup
		if (fs.existsSync(gpxPath)) fs.unlinkSync(gpxPath);
		if (fs.existsSync(fitPath)) fs.unlinkSync(fitPath);
	});
});

