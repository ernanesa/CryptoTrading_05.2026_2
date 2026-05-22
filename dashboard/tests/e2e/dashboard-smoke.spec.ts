import { expect, test } from '@playwright/test';

test('dashboard exposes hardening and risk views', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'Overview Performance Panel' })).toBeVisible();
  await expect(page.getByText('Hardening', { exact: true })).toBeVisible();
  await expect(page.getByText('Native AOT opt-in')).toBeVisible();
  await expect(page.getByText('API e Worker publicados via tools/validate-native-aot.sh.')).toBeVisible();

  await page.getByText('Gestão de Risco').click();

  await expect(page.getByRole('heading', { name: 'Risk Engine Security Controller' })).toBeVisible();
  await expect(page.getByText(/Status do RiskEngine:/)).toBeVisible();
});
