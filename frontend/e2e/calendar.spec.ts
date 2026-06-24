import { test, expect } from './fixtures';

/** Takvim: ızgara render + ay navigasyonu. */
test.describe('Takvim', () => {
  test('ızgara görünür ve ay navigasyonu çalışır', async ({ page }) => {
    await page.goto('/calendar');
    await expect(page.getByRole('heading', { name: 'Ziyaret Takvimi' }).first()).toBeVisible();
    await expect(page.locator('.cal-cell').first()).toBeVisible();

    // Sonraki/önceki ay (‹ ›) — çökmeden çalışmalı
    await page.getByRole('button', { name: '›' }).click();
    await page.getByRole('button', { name: '‹' }).click();
    await expect(page.locator('.cal-cell').first()).toBeVisible();
  });
});
