import { expect, test } from '@playwright/test';

test('dashboard exposes hardening and risk views', async ({ page }) => {
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

  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'Overview Performance Panel' })).toBeVisible();
  await expect(page.getByText('Runtime: Simulation')).toBeVisible();
  await expect(page.getByText('Hardening', { exact: true })).toBeVisible();
  await expect(page.getByText('Native AOT opt-in')).toBeVisible();
  await expect(page.getByText('API e Worker publicados via tools/validate-native-aot.sh.')).toBeVisible();

  await page.getByText('Gestão de Risco').click();

  await expect(page.getByRole('heading', { name: 'Risk Engine Security Controller' })).toBeVisible();
  await expect(page.getByText(/Status do RiskEngine:/)).toBeVisible();
});
