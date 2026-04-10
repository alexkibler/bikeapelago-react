import { test, expect } from '@playwright/test';
import { FitWriter } from '@markw65/fit-file-writer';
import * as fs from 'fs';
import * as path from 'path';

test('Capture Ride Summary Screenshot', async ({ page }) => {
	// 0. Enable Mock Mode for consistent summary UI
	await page.addInitScript(() => {
		(window as any).PLAYWRIGHT_TEST = true;
	});

	// 1. Start a New Game (Single Player)
	await page.goto('/new-game');
	await page.click('button:has-text("Single Player")');
	await page.click('button:has-text("Start Single Player")');

	// 2. Configure Session
	await page.waitForURL(/\/setup-session/);
	// Pittsburgh is the default center in SessionSetup.tsx
	await page.click('button:has-text("Create Session")');

	// 3. Wait for Game View
	await page.waitForURL(/\/game\//, { timeout: 60000 });
	const sessionId = page.url().split('/').pop();
	console.log(`Created session: ${sessionId}`);

	// 4. Generate a FIT file that will trigger at least one node check
	// We'll use points around the default Pittsburgh center: [40.4406, -79.9959]
	const toSemicircles = (deg: number) => Math.round(deg * (Math.pow(2, 31) / 180));
	const writer = new FitWriter();
	writer.writeMessage('file_id', {
		type: 'activity',
		manufacturer: 'development',
		product: 0,
		serial_number: 999,
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
		total_elapsed_time: 3600,
		total_timer_time: 3600,
		total_distance: 5000,
		total_ascent: 100
	});
	writer.writeMessage('lap', {
		timestamp: writer.time(startTime),
		start_time: writer.time(startTime),
		total_elapsed_time: 3600,
		total_timer_time: 3600,
		total_distance: 5000,
		total_ascent: 100
	});

	// Points around Pittsburgh
	for (let i = 0; i < 20; i++) {
		writer.writeMessage('record', {
			timestamp: writer.time(new Date(startTime.getTime() + i * 1000)),
			position_lat: toSemicircles(40.4406 + i * 0.0001),
			position_long: toSemicircles(-79.9959 + i * 0.0001),
			altitude: 250,
			distance: i * 50
		});
	}

	const fitData = writer.finish();
	const fitFilePath = path.join(process.cwd(), 'temp_capture.fit');
	fs.writeFileSync(
		fitFilePath,
		Buffer.from(fitData.buffer, fitData.byteOffset, fitData.byteLength)
	);

	// 5. Upload and Capture
	const uploadTab = page.locator('button:has-text("Upload")').filter({ visible: true });
	await uploadTab.click();

	await expect(page.locator('.panel-title')).toContainText('Upload', { timeout: 15000 });

	const fileInput = page.locator('input#file-upload');
	await fileInput.setInputFiles(fitFilePath);

	await page.click('button:has-text("Analyze Ride")');

	// Wait for summary UI
	await expect(page.locator('text=DISTANCE')).toBeVisible({ timeout: 30000 });

	// Wait for map and fonts
	await page.waitForLoadState('networkidle');
	await page.evaluate(() => document.fonts.ready);

	// Capture screenshot
	const screenshotPath = path.join(process.cwd(), 'static/docs/screenshots/7_Ride_Summary.png');
	// Ensure directory exists
	const dir = path.dirname(screenshotPath);
	if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
	
	await page.screenshot({ path: screenshotPath, fullPage: true });
	console.log(`Screenshot saved to ${screenshotPath}`);

	if (fs.existsSync(fitFilePath)) fs.unlinkSync(fitFilePath);
});

