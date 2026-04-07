import { test as setup, expect } from '@playwright/test';

const authFile = 'playwright/.auth/user.json';

setup('authenticate', async ({ page }) => {
  page.on('console', msg => console.log('BROWSER LOG:', msg.text()));
  page.on('pageerror', err => console.log('BROWSER ERROR:', err.message));

  console.log('Navigating to /login...');
  await page.goto('/login');
  
  console.log('Filling credentials...');
  await page.fill('#login-username', 'testuser');
  await page.fill('#login-password', 'Password');
  
  console.log('Submitting form...');
  await page.click('#login-submit');

  console.log('Waiting for URL /...');
  await page.waitForURL('/', { timeout: 60000 });
  
  console.log('Checking for Welcome text...');
  await expect(page.locator('text=Welcome')).toBeVisible({ timeout: 20000 });

  console.log('Saving storage state...');
  await page.context().storageState({ path: authFile });
});
