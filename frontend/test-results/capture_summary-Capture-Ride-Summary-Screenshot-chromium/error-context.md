# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: capture_summary.spec.ts >> Capture Ride Summary Screenshot
- Location: tests/e2e/capture_summary.spec.ts:6:1

# Error details

```
Test timeout of 30000ms exceeded.
```

```
Error: locator.click: Test timeout of 30000ms exceeded.
Call log:
  - waiting for locator('button:has-text("Upload")').filter({ visible: true })

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
  6   | test('Capture Ride Summary Screenshot', async ({ context, page }) => {
  7   | 	// Set auth cookie
  8   | 	await context.addCookies([
  9   | 		{
  10  | 			name: 'mock_pb_auth',
  11  | 			value: JSON.stringify({
  12  | 				token: 'mock_token',
  13  | 				model: {
  14  | 					id: 'mock_user_123',
  15  | 					username: 'mockuser',
  16  | 					email: 'mock@example.com'
  17  | 				}
  18  | 			}),
  19  | 			domain: 'localhost',
  20  | 			path: '/'
  21  | 		}
  22  | 	]);
  23  |
  24  | 	// Ensure mock mode
  25  | 	await page.addInitScript(() => {
  26  | 		(window as any).PLAYWRIGHT_TEST = true;
  27  | 	});
  28  |
  29  | 	await page.goto('/game/mock_session_123');
  30  | 	await page.waitForLoadState('networkidle');
  31  |
  32  | 	const connectButton = page.locator('button:has-text("Connect & Play")');
  33  | 	if (await connectButton.isVisible()) {
  34  | 		await connectButton.click();
  35  | 	}
  36  |
  37  | 	// Generate a valid FIT file
  38  | 	const toSemicircles = (deg: number) => Math.round(deg * (Math.pow(2, 31) / 180));
  39  | 	const writer = new FitWriter();
  40  | 	writer.writeMessage('file_id', {
  41  | 		type: 'activity',
  42  | 		manufacturer: 'development',
  43  | 		product: 0,
  44  | 		serial_number: 999,
  45  | 		time_created: writer.time(new Date())
  46  | 	});
  47  | 	const startTime = new Date();
  48  | 	writer.writeMessage('activity', {
  49  | 		timestamp: writer.time(startTime),
  50  | 		num_sessions: 1,
  51  | 		type: 'manual',
  52  | 		event: 'activity',
  53  | 		event_type: 'start'
  54  | 	});
  55  | 	writer.writeMessage('event', {
  56  | 		timestamp: writer.time(startTime),
  57  | 		event: 'timer',
  58  | 		event_type: 'start',
  59  | 		event_group: 0
  60  | 	});
  61  | 	writer.writeMessage('session', {
  62  | 		timestamp: writer.time(startTime),
  63  | 		start_time: writer.time(startTime),
  64  | 		sport: 'cycling',
  65  | 		total_elapsed_time: 3600,
  66  | 		total_timer_time: 3600,
  67  | 		total_distance: 25000,
  68  | 		total_ascent: 450
  69  | 	});
  70  | 	writer.writeMessage('lap', {
  71  | 		timestamp: writer.time(startTime),
  72  | 		start_time: writer.time(startTime),
  73  | 		total_elapsed_time: 3600,
  74  | 		total_timer_time: 3600,
  75  | 		total_distance: 25000,
  76  | 		total_ascent: 450
  77  | 	});
  78  |
  79  | 	// Path points around NYC center
  80  | 	for (let i = 0; i < 10; i++) {
  81  | 		writer.writeMessage('record', {
  82  | 			timestamp: writer.time(new Date(startTime.getTime() + i * 1000)),
  83  | 			position_lat: toSemicircles(40.7128 + i * 0.001),
  84  | 			position_long: toSemicircles(-74.006 + i * 0.001),
  85  | 			altitude: 250 + i * 2,
  86  | 			heart_rate: 140 + i,
  87  | 			power: 200 + i * 5,
  88  | 			distance: i * 100
  89  | 		});
  90  | 	}
  91  |
  92  | 	const fitData = writer.finish();
  93  | 	const fitFilePath = path.join(process.cwd(), 'temp_capture.fit');
  94  | 	fs.writeFileSync(
  95  | 		fitFilePath,
  96  | 		Buffer.from(fitData.buffer, fitData.byteOffset, fitData.byteLength)
  97  | 	);
  98  |
  99  | 	// Upload
  100 | 	const uploadTab = page.locator('button:has-text("Upload")').filter({ visible: true });
> 101 | 	await uploadTab.click();
      |                  ^ Error: locator.click: Test timeout of 30000ms exceeded.
  102 |
  103 | 	// Ensure panel is open
  104 | 	await expect(page.locator('.panel-title')).toContainText('Upload', { timeout: 15000 });
  105 |
  106 | 	const fileInput = page.locator('input#file-upload');
  107 | 	await fileInput.setInputFiles(fitFilePath);
  108 |
  109 | 	await page.click('button:has-text("Analyze Ride")');
  110 |
  111 | 	// Wait for summary UI
  112 | 	await expect(page.locator('text=Distance')).toBeVisible({ timeout: 15000 });
  113 |
  114 | 	// Wait for map zoom/polyline animations and network to settle
  115 | 	await page.waitForLoadState('networkidle');
  116 | 	await page.evaluate(() => document.fonts.ready);
  117 |
  118 | 	// Capture screenshot
  119 | 	const screenshotPath = path.join(process.cwd(), 'static/docs/screenshots/7_Ride_Summary.png');
  120 | 	await page.screenshot({ path: screenshotPath, fullPage: true });
  121 | 	console.log(`Screenshot saved to ${screenshotPath}`);
  122 |
  123 | 	if (fs.existsSync(fitFilePath)) fs.unlinkSync(fitFilePath);
  124 | });
  125 |
```