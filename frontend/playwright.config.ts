import { defineConfig, devices } from '@playwright/test';

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
			name: 'chromium',
			use: { ...devices['Desktop Chrome'] }
		},
		{
			name: 'mobile',
			use: {
				viewport: { width: 402, height: 874 },
				deviceScaleFactor: 3,
				isMobile: true,
				hasTouch: true,
				userAgent:
					'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1'
			}
		}
	],
	// Only run a local webserver if the target is localhost
	webServer: baseURL.includes('localhost')
		? {
				command: 'npm run dev -- --port 5174',
				url: 'http://localhost:5174',
				reuseExistingServer: true,
				timeout: 120 * 1000,
				env: {
					VITE_PUBLIC_GRAPHHOPPER_URL: 'https://routing.alexkibler.com/route',
					VITE_PUBLIC_DB_URL: 'https://pb.bikeapelago.alexkibler.com',
					VITE_PUBLIC_MOCK_MODE: 'true',
					VITE_PLAYWRIGHT_TEST: 'true'
				}
			}
		: undefined
});
