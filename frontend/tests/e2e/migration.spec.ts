import { test, expect } from '@playwright/test';
import fs from 'fs';
import path from 'path';

test.describe('Migration Verification', () => {
  test.slow();

  test('should login and create a new session', async ({ page }, testInfo) => {
    // Create a screenshots directory inside the test output folder
    const ssDir = testInfo.outputPath('screenshots');
    fs.mkdirSync(ssDir, { recursive: true });

    const snap = async (name: string) => {
      await page.screenshot({ path: path.join(ssDir, `${name}.png`) });
    };

    // 1. Login Page
    await page.goto('/login');
    await page.waitForSelector('#login-username', { timeout: 15000 });
    await page.fill('#login-username', 'testuser');
    await page.fill('#login-password', 'Password');
    await snap('01-login');
    await page.click('#login-submit');

    // 2. Home Page
    await page.waitForURL('**/', { timeout: 15000 });
    await page.waitForSelector('h1:has-text("Welcome Back")', { timeout: 15000 });
    await snap('02-home');

    // 3. New Game Page
    await page.click('a[href="/new-game"]');
    await page.waitForURL('**/new-game', { timeout: 15000 });
    await snap('03-new-game');

    // Select Single Player mode
    await page.click('button:has-text("Single Player")');
    await snap('03b-new-game-selected');

    // Start Single Player and wait for setup-session page
    await page.click('button:has-text("Start Single Player")');
    await page.waitForFunction(() => window.location.pathname.includes('/setup-session'), { timeout: 25000 });
    // Wait for map tiles to finish loading
    await page.waitForTimeout(4000);
    await snap('04-setup');

    // Create Session
    await page.click('button:has-text("Create Session")');
    await page.waitForFunction(() => window.location.pathname.includes('/game/'), { timeout: 30000 });
    // Wait for map tiles to finish loading
    await page.waitForTimeout(4000);
    await snap('05-game-view');

    // Rejoin Flow
    await page.goto('/');
    await page.waitForSelector('h1:has-text("Welcome Back")', { timeout: 15000 });
    const resumeBtn = page.locator('a:has-text("Resume Session")').first();
    await expect(resumeBtn).toBeVisible({ timeout: 10000 });
    await snap('06-home-rejoin');
    await resumeBtn.click();
    await page.waitForFunction(() => window.location.pathname.includes('/game/'), { timeout: 20000 });
    await page.waitForTimeout(4000);
    await snap('07-rejoined');
  });
});
