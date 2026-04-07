# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: migration.spec.ts >> Migration Verification >> should login and create a new session
- Location: tests/e2e/migration.spec.ts:8:3

# Error details

```
TimeoutError: page.waitForSelector: Timeout 15000ms exceeded.
Call log:
  - waiting for locator('#login-username') to be visible

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
  1  | import { test, expect } from '@playwright/test';
  2  | import fs from 'fs';
  3  | import path from 'path';
  4  | 
  5  | test.describe('Migration Verification', () => {
  6  |   test.slow();
  7  | 
  8  |   test('should login and create a new session', async ({ page }, testInfo) => {
  9  |     // Create a screenshots directory inside the test output folder
  10 |     const ssDir = testInfo.outputPath('screenshots');
  11 |     fs.mkdirSync(ssDir, { recursive: true });
  12 | 
  13 |     const snap = async (name: string) => {
  14 |       await page.screenshot({ path: path.join(ssDir, `${name}.png`) });
  15 |     };
  16 | 
  17 |     // 1. Login Page
  18 |     await page.goto('/login');
> 19 |     await page.waitForSelector('#login-username', { timeout: 15000 });
     |                ^ TimeoutError: page.waitForSelector: Timeout 15000ms exceeded.
  20 |     await page.fill('#login-username', 'testuser');
  21 |     await page.fill('#login-password', 'Password');
  22 |     await snap('01-login');
  23 |     await page.click('#login-submit');
  24 | 
  25 |     // 2. Home Page
  26 |     await page.waitForURL('**/', { timeout: 15000 });
  27 |     await page.waitForSelector('h1:has-text("Welcome Back")', { timeout: 15000 });
  28 |     await snap('02-home');
  29 | 
  30 |     // 3. New Game Page
  31 |     await page.click('a[href="/new-game"]');
  32 |     await page.waitForURL('**/new-game', { timeout: 15000 });
  33 |     await snap('03-new-game');
  34 | 
  35 |     // Select Single Player mode
  36 |     await page.click('button:has-text("Single Player")');
  37 |     await snap('03b-new-game-selected');
  38 | 
  39 |     // Start Single Player and wait for setup-session page
  40 |     await page.click('button:has-text("Start Single Player")');
  41 |     await page.waitForFunction(() => window.location.pathname.includes('/setup-session'), { timeout: 25000 });
  42 |     // Wait for map tiles to finish loading
  43 |     await page.waitForTimeout(4000);
  44 |     await snap('04-setup');
  45 | 
  46 |     // Create Session
  47 |     await page.click('button:has-text("Create Session")');
  48 |     await page.waitForFunction(() => window.location.pathname.includes('/game/'), { timeout: 30000 });
  49 |     // Wait for map tiles to finish loading
  50 |     await page.waitForTimeout(4000);
  51 |     await snap('05-game-view');
  52 | 
  53 |     // Rejoin Flow
  54 |     await page.goto('/');
  55 |     await page.waitForSelector('h1:has-text("Welcome Back")', { timeout: 15000 });
  56 |     const resumeBtn = page.locator('a:has-text("Resume Session")').first();
  57 |     await expect(resumeBtn).toBeVisible({ timeout: 10000 });
  58 |     await snap('06-home-rejoin');
  59 |     await resumeBtn.click();
  60 |     await page.waitForFunction(() => window.location.pathname.includes('/game/'), { timeout: 20000 });
  61 |     await page.waitForTimeout(4000);
  62 |     await snap('07-rejoined');
  63 |   });
  64 | });
  65 | 
```