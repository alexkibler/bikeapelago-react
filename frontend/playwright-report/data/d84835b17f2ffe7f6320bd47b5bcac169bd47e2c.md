# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: e2e_route_upload.spec.ts >> E2E Map Routing, GPX Download, FIT Conversion, and Upload >> complete routing to upload workflow
- Location: tests/e2e/e2e_route_upload.spec.ts:111:2

# Error details

```
Error: expect(locator).toBeVisible() failed

Locator: locator('button:has-text("Download GPX"), button:has-text("GPX"), #export-gpx').first()
Expected: visible
Timeout: 15000ms
Error: element(s) not found

Call log:
  - Expect "toBeVisible" with timeout 15000ms
  - waiting for locator('button:has-text("Download GPX"), button:has-text("GPX"), #export-gpx').first()

```

# Page snapshot

```yaml
- generic [ref=e3]:
  - generic [ref=e4]:
    - navigation [ref=e6]:
      - link "bikeapelago" [ref=e8] [cursor=pointer]:
        - /url: /
        - generic [ref=e9]: bikeapelago
    - main [ref=e10]:
      - generic [ref=e11]:
        - generic [ref=e12]:
          - generic [ref=e15]: Mock Seed • Mock Slot
          - generic [ref=e16]:
            - generic [ref=e17]:
              - generic [ref=e18]: TOTAL
              - generic [ref=e19]: "2"
            - generic [ref=e20]:
              - generic [ref=e21]: CHECKED
              - generic [ref=e22]: "0"
            - generic [ref=e23]:
              - generic [ref=e24]: AVail
              - generic [ref=e25]: "2"
        - generic [ref=e26]:
          - generic [ref=e28]:
            - generic:
              - generic:
                - button "Mock Node 1" [ref=e29] [cursor=pointer]
                - button "Mock Node 2" [ref=e31] [cursor=pointer]
            - generic:
              - generic [ref=e33]:
                - button "Zoom in" [ref=e34] [cursor=pointer]: +
                - button "Zoom out" [ref=e35] [cursor=pointer]: −
              - generic [ref=e36]:
                - link "Leaflet" [ref=e37] [cursor=pointer]:
                  - /url: https://leafletjs.com
                  - img [ref=e38]
                  - text: Leaflet
                - text: "| ©"
                - link "OpenStreetMap" [ref=e42] [cursor=pointer]:
                  - /url: https://www.openstreetmap.org/copyright
                - text: contributors ©
                - link "CARTO" [ref=e43] [cursor=pointer]:
                  - /url: https://carto.com/attributions
          - generic [ref=e44]:
            - generic [ref=e45]:
              - generic [ref=e46]: route
              - button [ref=e47] [cursor=pointer]:
                - img [ref=e48]
            - generic [ref=e51]:
              - generic [ref=e52]:
                - img [ref=e53]
                - heading "Route Builder" [level=2] [ref=e55]
              - generic [ref=e56]:
                - paragraph [ref=e57]: Click on the map to add waypoints and generate your custom cycling route.
                - generic [ref=e58]:
                  - generic [ref=e59]:
                    - generic [ref=e60]: Distance
                    - generic [ref=e61]: 0.00km
                  - generic [ref=e62]:
                    - generic [ref=e63]: Elevation
                    - generic [ref=e64]: 0m
              - generic [ref=e66]:
                - heading "Waypoints" [level=3] [ref=e67]
                - paragraph [ref=e69]: No waypoints added yet.
  - generic [ref=e71]:
    - link "Home" [ref=e72] [cursor=pointer]:
      - /url: /
      - img [ref=e73]
      - generic [ref=e76]: Home
    - button "Chat" [ref=e77] [cursor=pointer]:
      - img [ref=e78]
      - generic [ref=e80]: Chat
    - button "Upload .fit" [ref=e81] [cursor=pointer]:
      - img [ref=e82]
      - generic [ref=e85]: Upload .fit
    - button "Route Builder" [active] [ref=e86] [cursor=pointer]:
      - img [ref=e87]
      - generic [ref=e89]: Route Builder
```

# Test source

```ts
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
  124 | 		await page.locator('button:has-text("Route Builder")').filter({ visible: true }).click();
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
> 147 | 		await expect(downloadBtn).toBeVisible({ timeout: 15000 });
      |                             ^ Error: expect(locator).toBeVisible() failed
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