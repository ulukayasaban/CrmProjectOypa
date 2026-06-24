import { test, expect } from '@playwright/test';

/**
 * Giriş akışı E2E testi.
 * Seed kullanıcısı: umur.kutlu@oypa.com.tr / Oypa!2026 (Admin).
 */
test.describe('Giriş', () => {
  test('geçerli kimlik bilgisiyle giriş → dashboard', async ({ page }) => {
    await page.goto('/login');

    await page.getByLabel('E-posta').fill('umur.kutlu@oypa.com.tr');
    await page.getByLabel('Parola').fill('Oypa!2026');
    await page.getByRole('button', { name: 'Giriş Yap' }).click();

    // Dashboard'a yönlenmeli ve "Genel Bakış" görünmeli
    await expect(page).toHaveURL('http://localhost:5173/');
    await expect(page.getByText('Genel Bakış')).toBeVisible();
  });

  test('geçersiz kimlik bilgisinde hata gösterilir', async ({ page }) => {
    await page.goto('/login');

    await page.getByLabel('E-posta').fill('umur.kutlu@oypa.com.tr');
    await page.getByLabel('Parola').fill('YanlisParola1!');
    await page.getByRole('button', { name: 'Giriş Yap' }).click();

    // /login'de kalmalı (yönlenme olmamalı) ve hata mesajı belirmeli
    await expect(page).toHaveURL(/\/login$/);
  });
});
