import { test, expect } from '@playwright/test';
import { FitWriter } from '@markw65/fit-file-writer';
import * as fs from 'fs';
import * as path from 'path';

test.describe('Walkthrough Documentation Screenshots', () => {
	// Increase timeout for visual captures
	test.setTimeout(120000);

	const isMobile = process.env.PROJECT === 'mobile';
	const suffix = isMobile ? '_mobile' : '_desktop';

	async function capture(page: any, name: string) {
		// Wait for network/fonts to settle for visual capture
		await page.waitForLoadState('networkidle');
		await page.evaluate(() => document.fonts.ready);

		const screenshotPath = path.join(process.cwd(), `static/docs/screenshots/${name}${suffix}.png`);
		// Ensure directory exists
		const dir = path.dirname(screenshotPath);
		if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });

		await page.screenshot({ path: screenshotPath });
		console.log(`Saved screenshot: ${name}${suffix}.png`);
	}

	test.beforeEach(async ({ context, page }) => {
		// Set auth cookie
		await context.addCookies([
			{
				name: 'mock_pb_auth',
				value: JSON.stringify({
					token: 'mock_token',
					model: {
						id: 'mock_user_123',
						username: 'mockuser',
						email: 'mock@example.com'
					}
				}),
				domain: 'localhost',
				path: '/'
			}
		]);

		await page.addInitScript(() => {
			(window as any).PLAYWRIGHT_TEST = true;
		});
	});

	test('Capture complete gameplay walkthrough', async ({ page }) => {
		// 0. YAML Creator
		console.log('[Test] Capturing YAML Creator...');
		await page.goto('/yaml-creator');
		await page.waitForLoadState('networkidle');
		await page.fill('input#slotName', 'CyclingFan');
		await capture(page, '10_YAML_Creator');

		// 1. Dashboard
		console.log('[Test] Capturing Dashboard...');
		await page.goto('/');
		await page.waitForLoadState('networkidle');
		await capture(page, '1_Dashboard');

		// 2. New Game Form
		console.log('[Test] Capturing New Game Form...');
		await page.goto('/new-game');
		await page.waitForLoadState('networkidle');
		await capture(page, '2_NewGame_Form');

		// Fill form and search
		await page.fill('input#seedName', 'Documentation Seed');
		await page.fill('input#slotName', 'RiderOne');
		const addressInput = page.locator('input[placeholder="Search address or place…"]');
		await addressInput.click();
		await addressInput.type('New York, NY', { delay: 20 });
		await addressInput.press('Enter');
		await expect(page.locator('button:has-text("Search")')).toHaveText('Search', {
			timeout: 15000
		});

		// Wait for map or geocode results
		await page.waitForLoadState('networkidle');
		await capture(page, '3_NewGame_Map_Selected');

		// Generate and go to game
		await page.click('button:has-text("Generate Session")');
		await page.waitForURL('**/game/*', { timeout: 60000 });
		await page.waitForSelector('.leaflet-interactive', { timeout: 20000 });
		await page.waitForLoadState('networkidle');

		// Connect
		console.log('[Test] Connecting to session...');
		const connectButton = page.locator('button:has-text("Connect & Play")');
		if (await connectButton.isVisible()) {
			await connectButton.click();
		}
		// Wait for status pill to show connected state
		await expect(page.locator('.seed-label')).toBeVisible({ timeout: 20000 });
		await capture(page, '5_Game_Connected_State');

		// 4. Chat Tab
		console.log('[Test] Capturing Chat Tab...');
		await page.locator('button:has-text("Chat")').filter({ visible: true }).click();
		await capture(page, '8_Game_Chat_Tab');

		// 5. Route Tab
		console.log('[Test] Capturing Route Tab...');
		await page.locator('button:has-text("Route")').filter({ visible: true }).click();
		await page.waitForSelector('.node-item', { timeout: 15000 });
		await capture(page, '6_Game_Route_Tab');

		// 6. Upload Tab & Ride Summary
		console.log('[Test] Capturing Ride Summary...');
		await page.locator('button:has-text("Upload")').filter({ visible: true }).click();

		// Generate a FIT file for center of NYC
		const toSemicircles = (deg: number) => Math.round(deg * (Math.pow(2, 31) / 180));
		const writer = new FitWriter();
		writer.writeMessage('file_id', {
			type: 'activity',
			manufacturer: 'development',
			product: 0,
			serial_number: 555,
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
		writer.writeMessage('record', {
			timestamp: writer.time(startTime),
			position_lat: toSemicircles(40.7128),
			position_long: toSemicircles(-74.006),
			altitude: 250
		});
		const fitData = writer.finish();
		const fitFilePath = path.join(process.cwd(), 'temp_walkthrough.fit');
		fs.writeFileSync(
			fitFilePath,
			Buffer.from(fitData.buffer, fitData.byteOffset, fitData.byteLength)
		);

		await page.locator('input#file-upload').setInputFiles(fitFilePath);
		await page.click('button:has-text("Analyze Ride")');
		await expect(page.locator('text=Distance')).toBeVisible({ timeout: 20000 });
		await capture(page, '7_Ride_Summary');

		// 7. Unlocking Nodes
		console.log('[Test] Capturing Unlocking Nodes...');
		await page.click('button:has-text("Confirm & Send")');
		// Wait for network to settle after unlock and map refresh
		await page.waitForLoadState('networkidle');
		await capture(page, '8_Unlocking_Nodes');

		if (fs.existsSync(fitFilePath)) fs.unlinkSync(fitFilePath);
	});
});
