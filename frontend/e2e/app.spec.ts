import { test, expect } from '@playwright/test';
import { login } from './helpers';

/** Giriş gerektiren akışlar — her testten önce giriş yapılır. */
test.describe('Uygulama akışları', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('sidebar ile Potansiyel Müşteriler sayfasına gidiş', async ({ page }) => {
    await page.getByRole('link', { name: 'Potansiyel Müşteriler' }).click();
    await expect(page).toHaveURL(/\/leads$/);
  });

  test('İhaleler sayfasında arama sonucu filtreler', async ({ page }) => {
    await page.goto('/tenders/aktif');

    // Tablo yüklensin (seed: "Global Enerji ... İhalesi 2026" aktif segmentte)
    await expect(page.getByText('Global Enerji', { exact: false })).toBeVisible();

    // Arama: eşleşmeyen terim → "bulunamadı" boş durumu
    await page.getByRole('searchbox', { name: 'İhale ara' }).fill('zzz-eslesmeyen-terim');
    await expect(page.getByText('Bu kategoride ihale bulunamadı.')).toBeVisible();

    // Aramayı temizle → tekrar görünür
    await page.getByRole('searchbox', { name: 'İhale ara' }).fill('');
    await expect(page.getByText('Global Enerji', { exact: false })).toBeVisible();
  });

  test('profil sayfası kullanıcı bilgisini gösterir', async ({ page }) => {
    await page.goto('/profile');
    await expect(page.getByText('umur.kutlu@oypa.com.tr')).toBeVisible();
  });
});
