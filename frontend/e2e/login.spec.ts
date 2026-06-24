import { test, expect } from '@playwright/test';
import { ADMIN } from './helpers';

/**
 * Giriş akışı E2E testi.
 * Seed kullanıcısı: umur.kutlu@oypa.com.tr / Oypa!2026 (Admin).
 */
test.describe('Giriş', () => {
  test('geçerli kimlik bilgisiyle giriş → dashboard', async ({ page }) => {
    await page.goto('/login');

    await page.getByLabel('E-posta').fill(ADMIN.email);
    await page.getByLabel('Parola').fill(ADMIN.password);
    await page.getByRole('button', { name: 'Giriş Yap' }).click();

    // Dashboard'a yönlenmeli ve dashboard'a özgü içerik görünmeli
    await expect(page).toHaveURL('http://localhost:5173/');
    await expect(page.getByText('Aktif Leadler')).toBeVisible();
  });

  test('geçersiz kimlik bilgisinde login sayfasında kalır', async ({ page }) => {
    await page.goto('/login');

    await page.getByLabel('E-posta').fill(ADMIN.email);
    await page.getByLabel('Parola').fill('YanlisParola1!');
    await page.getByRole('button', { name: 'Giriş Yap' }).click();

    // Yönlenme olmamalı; /login'de kalmalı
    await expect(page).toHaveURL(/\/login$/);
    await expect(page.getByRole('button', { name: 'Giriş Yap' })).toBeVisible();
  });
});
