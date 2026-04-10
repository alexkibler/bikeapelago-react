import { test, expect } from '@playwright/test';
import * as path from 'path';

test.describe('Victory Screen Verification', () => {
	test.setTimeout(60000);

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

		page.on('console', (msg) => {
			console.log(`[Browser ${msg.type()}] ${msg.text()}`);
		});
	});

	test('should show victory screen when goal reached is triggered', async ({ page }, testInfo) => {
		const isMobile = testInfo.project.name === 'mobile';
		const suffix = isMobile ? '_mobile' : '_desktop';

		console.log('[Test] Navigating to game page...');
		await page.goto('/game/mock_session_123');
		await page.waitForLoadState('networkidle');

		// Connect first so map mounts
		const connectButton = page.locator('button:has-text("Connect & Play")');
		if (await connectButton.isVisible()) {
			await connectButton.click();
		}

		await page.waitForSelector('.leaflet-interactive', { timeout: 30000 });
		await page.waitForLoadState('networkidle');

		// CHEAT: Trigger the isGoalReached store directly!
		console.log('[Test] Triggering victory via store cheat...');
		await page.evaluate(() => {
			if ((window as any).isGoalReached) {
				(window as any).isGoalReached.set(true);
			} else {
				console.log('isGoalReached store not found on window');
			}
		});

		// Verify victory screen
		console.log('[Test] Waiting for Victory Screen...');
		const victoryHeading = page.locator('text=GOAL REACHED!');
		await expect(victoryHeading).toBeVisible({ timeout: 15000 });

		// Wait for network/animations to settle for visual capture
		await page.waitForLoadState('networkidle');
		await page.evaluate(() => document.fonts.ready);

		// Capture screenshot
		const screenshotPath = path.join(
			process.cwd(),
			`static/docs/screenshots/11_Victory_Screen${suffix}.png`
		);
		await page.screenshot({ path: screenshotPath });
		console.log(`Saved victory screenshot: 11_Victory_Screen${suffix}.png`);

		// Close it
		await page.click('button:has-text("HELL YEAH")');
		await expect(victoryHeading).not.toBeVisible();
	});
});
