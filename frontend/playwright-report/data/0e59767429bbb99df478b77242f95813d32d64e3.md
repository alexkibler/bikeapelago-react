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
  - generic [ref=e4]:
    - navigation [ref=e6]:
      - link "bikeapelago" [ref=e8] [cursor=pointer]:
        - /url: /
        - generic [ref=e9]: bikeapelago
    - main [ref=e10]:
      - generic [ref=e11]:
        - generic [ref=e12]:
          - generic [ref=e13]: Active Game Sessions
          - heading "Welcome Back, Rider" [level=1] [ref=e15]
          - paragraph [ref=e16]: Select a session to resume your journey through the Archipelago.
        - paragraph [ref=e17]: Failed to load sessions.
  - generic [ref=e19]:
    - link "Home" [ref=e20] [cursor=pointer]:
      - /url: /
      - img [ref=e21]
      - generic [ref=e24]: Home
    - link "Play" [ref=e25] [cursor=pointer]:
      - /url: /new-game
      - img [ref=e26]
      - generic [ref=e29]: Play
    - link "Create" [ref=e30] [cursor=pointer]:
      - /url: /yaml-creator
      - img [ref=e31]
      - generic [ref=e34]: Create
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