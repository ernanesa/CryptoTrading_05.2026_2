import { test, expect } from '@playwright/test';

test('Dashboard loads and shows Simulation Mode', async ({ page }) => {
  await page.goto('http://localhost:5173');
  await expect(page.locator('text=Simulation Mode').first()).toBeVisible();
});
