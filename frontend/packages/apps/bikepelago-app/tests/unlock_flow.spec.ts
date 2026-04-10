import { test, expect } from '@playwright/test';
import { FitWriter } from '@markw65/fit-file-writer';
import * as fs from 'fs';
import * as path from 'path';

test('Verify .fit upload unlocks new nodes on the map', async ({ page }, testInfo) => {
	// Enable browser logging for debugging
	page.on('console', (msg) => {
		console.log(`[Browser ${msg.type()}] ${msg.text()}`);
	});

	// Ensure mock mode
	await page.addInitScript(() => {
		(window as any).PLAYWRIGHT_TEST = true;
	});

	await page.goto('/game/mock_session_123');
	await page.waitForLoadState('networkidle');

	const connectButton = page.locator('button:has-text("Connect & Play")');
	if (await connectButton.isVisible()) {
		await connectButton.click();
	}

	// Open the panel if it's not already open (especially on mobile)
	const uploadTab = page.locator('button:has-text("Upload")').filter({ visible: true });
	await uploadTab.click();
	await expect(page.locator('.panel-title')).toContainText('Upload', { timeout: 15000 });

	// 1. Initial State: Identify an available node
	const centerNode = { lat: 40.7128, lon: -74.006 };

	// 2. Count Available nodes before upload
	await page.locator('button:has-text("Route")').filter({ visible: true }).click();
	await page.waitForSelector('.node-item', { timeout: 10000 });
	const initialAvailableCount = await page.locator('.node-item').count();
	console.log(`[Test] Initial available nodes listed: ${initialAvailableCount}`);

	// 3. Generate and Upload FIT file
	const toSemicircles = (deg: number) => Math.round(deg * (Math.pow(2, 31) / 180));
	const writer = new FitWriter();
	writer.writeMessage('file_id', {
		type: 'activity',
		manufacturer: 'development',
		product: 0,
		serial_number: 777,
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
	writer.writeMessage('session', {
		timestamp: writer.time(startTime),
		start_time: writer.time(startTime),
		sport: 'cycling',
		total_elapsed_time: 10,
		total_timer_time: 10,
		total_distance: 100,
		total_ascent: 5
	});
	writer.writeMessage('lap', {
		timestamp: writer.time(startTime),
		start_time: writer.time(startTime),
		total_elapsed_time: 10,
		total_timer_time: 10,
		total_distance: 100,
		total_ascent: 5
	});
	// Hit the center node (node_2 in mock)
	writer.writeMessage('record', {
		timestamp: writer.time(startTime),
		position_lat: toSemicircles(centerNode.lat),
		position_long: toSemicircles(centerNode.lon),
		altitude: 250
	});
	const fitData = writer.finish();
	const fitFilePath = path.join(process.cwd(), 'temp_unlock_test.fit');
	fs.writeFileSync(
		fitFilePath,
		Buffer.from(fitData.buffer, fitData.byteOffset, fitData.byteLength)
	);

	await page.locator('button:has-text("Upload")').filter({ visible: true }).click();
	await page.locator('input#file-upload').setInputFiles(fitFilePath);
	await page.click('button:has-text("Analyze Ride")');

	// Verify the check is found
	await expect(page.locator('.text-green-400')).toContainText('Check #');

	// 4. Confirm and wait for Mock Unlock
	await page.click('button:has-text("Confirm & Send")');
	await expect(page.locator('text=Successfully validated')).toBeVisible({ timeout: 15000 });

	// 5. Verify: Check the Route tab again
	await page.locator('button:has-text("Route")').filter({ visible: true }).click();

	// Wait for the mock unlock to complete by checking the count
	// In Mock Mode: 1 node Checked, 1 node Unlocked -> count remains same
	await expect(page.locator('.node-item')).toHaveCount(initialAvailableCount, { timeout: 10000 });

	const finalAvailableCount = await page.locator('.node-item').count();
	console.log(`[Test] Final available nodes listed: ${finalAvailableCount}`);

	expect(finalAvailableCount).toBeGreaterThan(0);

	// 6. Capture screenshot
	const suffix = testInfo.project.name === 'mobile' ? '_mobile' : '_desktop';
	const screenshotPath = path.join(
		process.cwd(),
		`static/docs/screenshots/8_Unlocking_Nodes${suffix}.png`
	);
	await page.screenshot({ path: screenshotPath, fullPage: true });
	console.log(`[Test] Screenshot saved to ${screenshotPath}`);

	if (fs.existsSync(fitFilePath)) fs.unlinkSync(fitFilePath);
});
