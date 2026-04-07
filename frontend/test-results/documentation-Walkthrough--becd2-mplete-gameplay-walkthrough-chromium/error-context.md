# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: documentation.spec.ts >> Walkthrough Documentation Screenshots >> Capture complete gameplay walkthrough
- Location: tests/e2e/documentation.spec.ts:50:2

# Error details

```
Test timeout of 120000ms exceeded.
```

```
Error: page.fill: Test timeout of 120000ms exceeded.
Call log:
  - waiting for locator('input#slotName')

```

# Page snapshot

```yaml
- generic [ref=e3]:
  - generic [ref=e4]: "[plugin:vite:import-analysis] Failed to resolve import \"../../../../src/lib/mock\" from \"src/store/authStore.ts\". Does the file exist?"
  - generic [ref=e5]: /app/frontend/src/store/authStore.ts:3:31
  - generic [ref=e6]: "1 | import { create } from \"zustand\"; 2 | import PocketBase from \"pocketbase\"; 3 | import { MockPocketBase } from \"../../../../src/lib/mock\"; | ^ 4 | // Point PocketBase to the .NET proxy rather than directly to the DB container 5 | const url = import.meta.env.VITE_PUBLIC_API_URL ? `${import.meta.env.VITE_PUBLIC_API_URL}/api/pb` : \"/api/pb\";"
  - generic [ref=e7]: at TransformPluginContext._formatLog (file:///app/frontend/node_modules/vite/dist/node/chunks/node.js:30339:39) at TransformPluginContext.error (file:///app/frontend/node_modules/vite/dist/node/chunks/node.js:30336:14) at normalizeUrl (file:///app/frontend/node_modules/vite/dist/node/chunks/node.js:27624:18) at async file:///app/frontend/node_modules/vite/dist/node/chunks/node.js:27687:30 at async Promise.all (index 2) at async TransformPluginContext.transform (file:///app/frontend/node_modules/vite/dist/node/chunks/node.js:27655:4) at async EnvironmentPluginContainer.transform (file:///app/frontend/node_modules/vite/dist/node/chunks/node.js:30128:14) at async loadAndTransform (file:///app/frontend/node_modules/vite/dist/node/chunks/node.js:24459:26) at async viteTransformMiddleware (file:///app/frontend/node_modules/vite/dist/node/chunks/node.js:24253:20)
  - generic [ref=e8]:
    - text: Click outside, press Esc key, or fix the code to dismiss.
    - text: You can also disable this overlay by setting
    - code [ref=e9]: server.hmr.overlay
    - text: to
    - code [ref=e10]: "false"
    - text: in
    - code [ref=e11]: vite.config.ts
    - text: .
```

# Test source

```ts
  1   | import { test, expect } from '@playwright/test';
  2   | import { FitWriter } from '@markw65/fit-file-writer';
  3   | import * as fs from 'fs';
  4   | import * as path from 'path';
  5   |
  6   | test.describe('Walkthrough Documentation Screenshots', () => {
  7   | 	// Increase timeout for visual captures
  8   | 	test.setTimeout(120000);
  9   |
  10  | 	const isMobile = process.env.PROJECT === 'mobile';
  11  | 	const suffix = isMobile ? '_mobile' : '_desktop';
  12  |
  13  | 	async function capture(page: any, name: string) {
  14  | 		// Wait for network/fonts to settle for visual capture
  15  | 		await page.waitForLoadState('networkidle');
  16  | 		await page.evaluate(() => document.fonts.ready);
  17  |
  18  | 		const screenshotPath = path.join(process.cwd(), `static/docs/screenshots/${name}${suffix}.png`);
  19  | 		// Ensure directory exists
  20  | 		const dir = path.dirname(screenshotPath);
  21  | 		if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
  22  |
  23  | 		await page.screenshot({ path: screenshotPath });
  24  | 		console.log(`Saved screenshot: ${name}${suffix}.png`);
  25  | 	}
  26  |
  27  | 	test.beforeEach(async ({ context, page }) => {
  28  | 		// Set auth cookie
  29  | 		await context.addCookies([
  30  | 			{
  31  | 				name: 'mock_pb_auth',
  32  | 				value: JSON.stringify({
  33  | 					token: 'mock_token',
  34  | 					model: {
  35  | 						id: 'mock_user_123',
  36  | 						username: 'mockuser',
  37  | 						email: 'mock@example.com'
  38  | 					}
  39  | 				}),
  40  | 				domain: 'localhost',
  41  | 				path: '/'
  42  | 			}
  43  | 		]);
  44  |
  45  | 		await page.addInitScript(() => {
  46  | 			(window as any).PLAYWRIGHT_TEST = true;
  47  | 		});
  48  | 	});
  49  |
  50  | 	test('Capture complete gameplay walkthrough', async ({ page }) => {
  51  | 		// 0. YAML Creator
  52  | 		console.log('[Test] Capturing YAML Creator...');
  53  | 		await page.goto('/yaml-creator');
  54  | 		await page.waitForLoadState('networkidle');
> 55  | 		await page.fill('input#slotName', 'CyclingFan');
      |              ^ Error: page.fill: Test timeout of 120000ms exceeded.
  56  | 		await capture(page, '10_YAML_Creator');
  57  |
  58  | 		// 1. Dashboard
  59  | 		console.log('[Test] Capturing Dashboard...');
  60  | 		await page.goto('/');
  61  | 		await page.waitForLoadState('networkidle');
  62  | 		await capture(page, '1_Dashboard');
  63  |
  64  | 		// 2. New Game Form
  65  | 		console.log('[Test] Capturing New Game Form...');
  66  | 		await page.goto('/new-game');
  67  | 		await page.waitForLoadState('networkidle');
  68  | 		await capture(page, '2_NewGame_Form');
  69  |
  70  | 		// Fill form and search
  71  | 		await page.fill('input#seedName', 'Documentation Seed');
  72  | 		await page.fill('input#slotName', 'RiderOne');
  73  | 		const addressInput = page.locator('input[placeholder="Search address or place…"]');
  74  | 		await addressInput.click();
  75  | 		await addressInput.type('New York, NY', { delay: 20 });
  76  | 		await addressInput.press('Enter');
  77  | 		await expect(page.locator('button:has-text("Search")')).toHaveText('Search', {
  78  | 			timeout: 15000
  79  | 		});
  80  |
  81  | 		// Wait for map or geocode results
  82  | 		await page.waitForLoadState('networkidle');
  83  | 		await capture(page, '3_NewGame_Map_Selected');
  84  |
  85  | 		// Generate and go to game
  86  | 		await page.click('button:has-text("Generate Session")');
  87  | 		await page.waitForURL('**/game/*', { timeout: 60000 });
  88  | 		await page.waitForSelector('.leaflet-interactive', { timeout: 20000 });
  89  | 		await page.waitForLoadState('networkidle');
  90  |
  91  | 		// Connect
  92  | 		console.log('[Test] Connecting to session...');
  93  | 		const connectButton = page.locator('button:has-text("Connect & Play")');
  94  | 		if (await connectButton.isVisible()) {
  95  | 			await connectButton.click();
  96  | 		}
  97  | 		// Wait for status pill to show connected state
  98  | 		await expect(page.locator('.seed-label')).toBeVisible({ timeout: 20000 });
  99  | 		await capture(page, '5_Game_Connected_State');
  100 |
  101 | 		// 4. Chat Tab
  102 | 		console.log('[Test] Capturing Chat Tab...');
  103 | 		await page.locator('button:has-text("Chat")').filter({ visible: true }).click();
  104 | 		await capture(page, '8_Game_Chat_Tab');
  105 |
  106 | 		// 5. Route Tab
  107 | 		console.log('[Test] Capturing Route Tab...');
  108 | 		await page.locator('button:has-text("Route")').filter({ visible: true }).click();
  109 | 		await page.waitForSelector('.node-item', { timeout: 15000 });
  110 | 		await capture(page, '6_Game_Route_Tab');
  111 |
  112 | 		// 6. Upload Tab & Ride Summary
  113 | 		console.log('[Test] Capturing Ride Summary...');
  114 | 		await page.locator('button:has-text("Upload")').filter({ visible: true }).click();
  115 |
  116 | 		// Generate a FIT file for center of NYC
  117 | 		const toSemicircles = (deg: number) => Math.round(deg * (Math.pow(2, 31) / 180));
  118 | 		const writer = new FitWriter();
  119 | 		writer.writeMessage('file_id', {
  120 | 			type: 'activity',
  121 | 			manufacturer: 'development',
  122 | 			product: 0,
  123 | 			serial_number: 555,
  124 | 			time_created: writer.time(new Date())
  125 | 		});
  126 | 		const startTime = new Date();
  127 | 		writer.writeMessage('activity', {
  128 | 			timestamp: writer.time(startTime),
  129 | 			num_sessions: 1,
  130 | 			type: 'manual',
  131 | 			event: 'activity',
  132 | 			event_type: 'start'
  133 | 		});
  134 | 		writer.writeMessage('event', {
  135 | 			timestamp: writer.time(startTime),
  136 | 			event: 'timer',
  137 | 			event_type: 'start',
  138 | 			event_group: 0
  139 | 		});
  140 | 		writer.writeMessage('session', {
  141 | 			timestamp: writer.time(startTime),
  142 | 			start_time: writer.time(startTime),
  143 | 			sport: 'cycling',
  144 | 			total_elapsed_time: 10,
  145 | 			total_timer_time: 10,
  146 | 			total_distance: 100,
  147 | 			total_ascent: 5
  148 | 		});
  149 | 		writer.writeMessage('lap', {
  150 | 			timestamp: writer.time(startTime),
  151 | 			start_time: writer.time(startTime),
  152 | 			total_elapsed_time: 10,
  153 | 			total_timer_time: 10,
  154 | 			total_distance: 100,
  155 | 			total_ascent: 5
```