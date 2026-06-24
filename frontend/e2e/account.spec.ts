import { test, expect } from '@playwright/test';

/** Hesap kurtarma akışları (anonim sayfalar — giriş gerektirmez). */
test.describe('Hesap kurtarma', () => {
  test('login → "Şifremi Unuttum?" linki forgot-password sayfasına gider', async ({ page }) => {
    await page.goto('/login');
    await page.getByRole('link', { name: 'Şifremi Unuttum?' }).click();
    await expect(page).toHaveURL(/\/forgot-password$/);
  });

  test('forgot-password gönderince nötr bilgi mesajı gösterir', async ({ page }) => {
    await page.goto('/forgot-password');
    await page.getByPlaceholder('ornek@oypa.com.tr').fill('umur.kutlu@oypa.com.tr');
    await page.getByRole('button', { name: 'Sıfırlama Bağlantısı Gönder' }).click();
    await expect(
      page.getByText('Eğer bu e-posta kayıtlıysa sıfırlama bağlantısı gönderildi.'),
    ).toBeVisible();
  });

  test('reset-password token yoksa geçersiz bağlantı uyarısı', async ({ page }) => {
    await page.goto('/reset-password');
    await expect(page.getByText('Geçersiz veya eksik sıfırlama bağlantısı.')).toBeVisible();
  });
});
