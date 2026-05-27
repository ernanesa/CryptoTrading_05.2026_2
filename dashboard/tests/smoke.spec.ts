import { test, expect } from '@playwright/test';

test('Dashboard loads and shows Simulation Mode', async ({ page }) => {
  await page.route('**/api/runtime/status', async route => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({
        mode: 'Simulation',
        isSimulation: true,
        isPaper: false,
        isTestnet: false,
        isRealTestnet: false,
        warnings: [],
        timestamp: new Date().toISOString()
      })
    });
  });

  await page.goto('http://localhost:5173');
  await expect(page.locator('text=Simulation Mode').first()).toBeVisible();
  await expect(page.getByText('Runtime: Simulation')).toBeVisible();
});
