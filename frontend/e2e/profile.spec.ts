import { test, expect } from './fixtures';

/** Profil görüntüleme + düzenleme/parola modalları (açılış; gönderim yapılmaz). */
test.describe('Profil', () => {
  test('profil bilgisi gösterilir', async ({ page }) => {
    await page.goto('/profile');
    await expect(page.getByText('umur.kutlu@oypa.com.tr')).toBeVisible();
  });

  test('Profili Düzenle modalı açılır', async ({ page }) => {
    await page.goto('/profile');
    await page.getByRole('button', { name: 'Profili Düzenle' }).click();
    await expect(page.getByRole('dialog', { name: 'Profili Düzenle' })).toBeVisible();
    await page.getByRole('button', { name: 'Kapat' }).click();
    await expect(page.getByRole('dialog', { name: 'Profili Düzenle' })).toHaveCount(0);
  });

  test('Parolayı Değiştir modalı açılır', async ({ page }) => {
    await page.goto('/profile');
    await page.getByRole('button', { name: 'Parolayı Değiştir' }).click();
    await expect(page.getByRole('dialog', { name: 'Parolayı Değiştir' })).toBeVisible();
  });
});
