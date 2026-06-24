import { test, expect } from './fixtures';

/** Bildirim çanı popover'ı. */
test.describe('Bildirimler', () => {
  test('çana tıklayınca popover açılır', async ({ page }) => {
    await page.getByRole('button', { name: 'Bildirimler' }).click();
    // Popover başlığı görünür
    await expect(page.getByRole('heading', { name: 'Bildirimler' })).toBeVisible();
    // "Tümünü Okundu Yap" aksiyonu mevcut
    await expect(page.getByRole('button', { name: 'Tümünü Okundu Yap' })).toBeVisible();
  });
});
