# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: e2e_route_upload.spec.ts >> E2E Map Routing, GPX Download, FIT Conversion, and Upload >> complete routing to upload workflow
- Location: tests/e2e/e2e_route_upload.spec.ts:111:2

# Error details

```
Test timeout of 180000ms exceeded.
```

```
Error: locator.click: Test timeout of 180000ms exceeded.
Call log:
  - waiting for locator('button:has-text("Route Builder")').filter({ visible: true })

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
  24  | 			}
  25  | 		]);
  26  |
  27  | 		page.on('console', (msg) => {
  28  | 			if (msg.type() === 'error') console.error(`[Browser Error] ${msg.text()}`);
  29  | 		});
  30  |
  31  | 		// Ensure mock mode
  32  | 		await page.addInitScript(() => {
  33  | 			(window as any).PLAYWRIGHT_TEST = true;
  34  | 		});
  35  | 	});
  36  |
  37  | 	function createFitFileFromGpx(gpxPath: string, fitPath: string) {
  38  | 		const gpxContent = fs.readFileSync(gpxPath, 'utf8');
  39  | 		const regex = /<trkpt lat="([^"]+)" lon="([^"]+)">/g;
  40  | 		const points: {lat: number, lon: number}[] = [];
  41  | 		let match;
  42  | 		while ((match = regex.exec(gpxContent)) !== null) {
  43  | 			points.push({
  44  | 				lat: parseFloat(match[1]),
  45  | 				lon: parseFloat(match[2])
  46  | 			});
  47  | 		}
  48  |
  49  | 		if (points.length === 0) {
  50  | 			throw new Error('No track points found in GPX file');
  51  | 		}
  52  |
  53  | 		const toSemicircles = (deg: number) => Math.round(deg * (Math.pow(2, 31) / 180));
  54  | 		const writer = new FitWriter();
  55  | 		writer.writeMessage('file_id', {
  56  | 			type: 'activity',
  57  | 			manufacturer: 'development',
  58  | 			product: 0,
  59  | 			serial_number: 123,
  60  | 			time_created: writer.time(new Date())
  61  | 		});
  62  |
  63  | 		const startTime = new Date();
  64  | 		writer.writeMessage('activity', {
  65  | 			timestamp: writer.time(startTime),
  66  | 			num_sessions: 1,
  67  | 			type: 'manual',
  68  | 			event: 'activity',
  69  | 			event_type: 'start'
  70  | 		});
  71  | 		writer.writeMessage('event', {
  72  | 			timestamp: writer.time(startTime),
  73  | 			event: 'timer',
  74  | 			event_type: 'start',
  75  | 			event_group: 0
  76  | 		});
  77  | 		writer.writeMessage('session', {
  78  | 			timestamp: writer.time(startTime),
  79  | 			start_time: writer.time(startTime),
  80  | 			sport: 'cycling',
  81  | 			total_elapsed_time: points.length * 10,
  82  | 			total_timer_time: points.length * 10,
  83  | 			total_distance: 100 * points.length,
  84  | 			total_ascent: 50
  85  | 		});
  86  | 		writer.writeMessage('lap', {
  87  | 			timestamp: writer.time(startTime),
  88  | 			start_time: writer.time(startTime),
  89  | 			total_elapsed_time: points.length * 10,
  90  | 			total_timer_time: points.length * 10,
  91  | 			total_distance: 100 * points.length,
  92  | 			total_ascent: 50
  93  | 		});
  94  |
  95  | 		points.forEach((pt, i) => {
  96  | 			writer.writeMessage('record', {
  97  | 				timestamp: writer.time(new Date(startTime.getTime() + i * 1000)),
  98  | 				position_lat: toSemicircles(pt.lat),
  99  | 				position_long: toSemicircles(pt.lon),
  100 | 				altitude: 250,
  101 | 				distance: i * 100,
  102 | 				heart_rate: 140,
  103 | 				power: 200
  104 | 			});
  105 | 		});
  106 |
  107 | 		const fitData = writer.finish();
  108 | 		fs.writeFileSync(fitPath, Buffer.from(fitData.buffer, fitData.byteOffset, fitData.byteLength));
  109 | 	}
  110 |
  111 | 	test('complete routing to upload workflow', async ({ page }) => {
  112 | 		console.log('Navigating to mock session...');
  113 | 		await page.goto('/game/mock_session_123');
  114 | 		await page.waitForLoadState('networkidle');
  115 | 		const isMobile = page.viewportSize()!.width < 768;
  116 |
  117 | 		const connectButton = page.locator('button:has-text("Connect & Play")');
  118 | 		if (await connectButton.isVisible()) {
  119 | 			await connectButton.click();
  120 | 		}
  121 |
  122 | 		console.log('Opening Route Builder tab...');
  123 | 		// Make sure Route Builder is active
> 124 | 		await page.locator('button:has-text("Route Builder")').filter({ visible: true }).click();
      |                                                                                    ^ Error: locator.click: Test timeout of 180000ms exceeded.
  125 | 		await expect(page.locator('.panel-title')).toContainText('Route Builder', { timeout: 15000 });
  126 |
  127 | 		if (isMobile) {
  128 | 			console.log('Mobile: Closing panel to allow map click');
  129 | 			await page.locator('button:has-text("Route Builder")').filter({ visible: true }).click(); // Toggle off
  130 | 		}
  131 |
  132 | 		console.log('Clicking on map to generate route...');
  133 | 		// Click two points on the map
  134 | 		await page.locator('.leaflet-container').click({ position: { x: 150, y: 150 } });
  135 | 		await page.waitForTimeout(500);
  136 | 		await page.locator('.leaflet-container').click({ position: { x: 300, y: 300 } });
  137 |
  138 | 		if (isMobile) {
  139 | 			console.log('Mobile: Re-opening Route Builder to see stats');
  140 | 			await page.locator('button:has-text("Route Builder")').filter({ visible: true }).click();
  141 | 		}
  142 |
  143 | 		console.log('Waiting for Download GPX button...');
  144 | 		// The original button has an ID #export-gpx or text "GPX"
  145 | 		// Wait for routing to complete. The 'Download GPX' button should appear in the Route Builder panel or above map.
  146 | 		const downloadBtn = page.locator('button:has-text("Download GPX"), button:has-text("GPX"), #export-gpx').first();
  147 | 		await expect(downloadBtn).toBeVisible({ timeout: 15000 });
  148 |
  149 | 		console.log('Downloading GPX...');
  150 | 		const downloadPromise = page.waitForEvent('download');
  151 | 		await downloadBtn.click();
  152 | 		const download = await downloadPromise;
  153 |
  154 | 		const gpxPath = path.join(process.cwd(), 'temp_route.gpx');
  155 | 		await download.saveAs(gpxPath);
  156 |
  157 | 		expect(fs.existsSync(gpxPath)).toBeTruthy();
  158 | 		console.log('GPX downloaded successfully.');
  159 |
  160 | 		console.log('Converting GPX to FIT...');
  161 | 		const fitPath = path.join(process.cwd(), 'temp_route.fit');
  162 | 		createFitFileFromGpx(gpxPath, fitPath);
  163 | 		expect(fs.existsSync(fitPath)).toBeTruthy();
  164 | 		console.log('FIT file created successfully.');
  165 |
  166 | 		console.log('Switching to Upload tab...');
  167 | 		await page.locator('button:has-text("Upload")').filter({ visible: true }).click();
  168 | 		await expect(page.locator('.panel-title')).toContainText('Upload', { timeout: 15000 });
  169 |
  170 | 		console.log('Uploading FIT file...');
  171 | 		await page.locator('input#file-upload').setInputFiles(fitPath);
  172 |
  173 | 		console.log('Analyzing ride...');
  174 | 		const analyzeBtn = page.locator('button:has-text("Analyze Ride")');
  175 | 		await expect(analyzeBtn).toBeVisible();
  176 | 		await analyzeBtn.click();
  177 |
  178 | 		// Wait for summary to show DISTANCE (uppercase in UI)
  179 | 		await expect(page.locator('text=DISTANCE')).toBeVisible({ timeout: 15000 });
  180 |
  181 | 		// We know it might have cleared checks or zero checks depending on mock logic, but let's confirm & send
  182 | 		// and assert the success message appears.
  183 | 		console.log('Confirming and sending...');
  184 | 		const confirmBtn = page.locator('button:has-text("Confirm & Send")');
  185 | 		// We might need to ensure it's not disabled if there's no locations. In mock mode, locations could be anywhere.
  186 | 		// If it's disabled due to 0 locations, we just report it.
  187 | 		if (await confirmBtn.isDisabled()) {
  188 |             console.log('Confirm button is disabled, likely 0 locations reached in mock mock.');
  189 |         } else {
  190 |             await confirmBtn.click();
  191 |             await expect(page.locator('text=Successfully validated')).toBeVisible({ timeout: 15000 });
  192 |             console.log('Ride validated successfully.');
  193 |         }
  194 |
  195 | 		// Cleanup
  196 | 		if (fs.existsSync(gpxPath)) fs.unlinkSync(gpxPath);
  197 | 		if (fs.existsSync(fitPath)) fs.unlinkSync(fitPath);
  198 | 	});
  199 | });
  200 |
```