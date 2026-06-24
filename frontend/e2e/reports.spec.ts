import { test, expect } from './fixtures';

/** Raporlar sayfası — indirme kartları/butonları görünür.
 *  (Gerçek dosya üretimi backend integration testlerinde doğrulanır.) */
test.describe('Raporlar', () => {
  test('Görüşme ve İhale raporu indirme butonları görünür', async ({ page }) => {
    await page.goto('/raporlar');
    await expect(page.getByText('Görüşme Raporu (Excel)')).toBeVisible();
    await expect(page.getByText('İhale Raporu (Excel)')).toBeVisible();
    await expect(page.getByRole('button', { name: 'İndir' })).toHaveCount(2);
  });
});
