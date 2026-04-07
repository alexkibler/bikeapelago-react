import { defineConfig, devices } from '@playwright/test';
import process from 'node:process';

const baseURL = process.env.BASE_URL || process.env.TEST_BASE_URL || 'http://localhost:5174';

/**
 * E2E test configuration for Bikeapelago.
 */
export default defineConfig({
	testDir: './tests/e2e',
	fullyParallel: false,
	forbidOnly: !!process.env.CI,
	retries: 0,
	workers: 1,
	reporter: [['html', { outputFolder: 'playwright-report' }], ['list']],
	use: {
		baseURL,
		trace: 'on-first-retry',
		screenshot: 'only-on-failure',
		video: 'off'
	},
	projects: [
		{
			name: 'setup',
			testMatch: /auth\.setup\.ts/,
		},
		{
			name: 'chromium',
			use: { 
				...devices['Desktop Chrome'],
				storageState: 'playwright/.auth/user.json',
			},
			dependencies: ['setup']
		},
		{
			name: 'mobile',
			use: {
				viewport: { width: 402, height: 874 },
				deviceScaleFactor: 3,
				isMobile: true,
				hasTouch: true,
				userAgent:
					'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1',
				storageState: 'playwright/.auth/user.json',
			},
			dependencies: ['setup']
		}
	],
	// Only run a local webserver if the target is localhost
	webServer: baseURL.includes('localhost')
		? [
				{
					command: 'dotnet run --project ../api/Bikeapelago.Api.csproj --launch-profile http',
					url: 'http://localhost:5054/api/pb/api/health',  // Ensure we wait for a valid API endpoint, or just http://localhost:5054 isn't enough maybe? Let's stick with localhost.
					reuseExistingServer: true,
					timeout: 120 * 1000,
					env: {
						USE_MOCK_OVERPASS: 'true'
					}
				},
				{
					command: 'npm run dev -- --port 5174',
					url: 'http://localhost:5174',
					reuseExistingServer: true,
					timeout: 120 * 1000,
					env: {
						VITE_PUBLIC_GRAPHHOPPER_URL: 'https://routing.alexkibler.com/route',
						VITE_PUBLIC_DB_URL: 'https://pb.bikeapelago.alexkibler.com',
						VITE_PUBLIC_API_URL: 'http://localhost:5054',
						VITE_PLAYWRIGHT_TEST: 'true'
					}
				}
		  ]
		: undefined
});
