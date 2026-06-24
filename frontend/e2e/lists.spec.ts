import { test, expect } from './fixtures';

/** Liste sayfalarında arama kutusu ve boş durum davranışı. */
test.describe('Listeler', () => {
  test('Lead aramada eşleşme yoksa boş durum gösterilir', async ({ page }) => {
    await page.goto('/leads');
    const search = page.getByLabel('Lead ara');
    await expect(search).toBeVisible();
    await search.fill('zzz-eslesmeyen-terim-xyz');
    await expect(page.getByText(/bulunamadı/i)).toBeVisible();
  });

  test('Müşteri sayfasında arama kutusu var', async ({ page }) => {
    await page.goto('/customers/aktif');
    await expect(page.getByLabel('Müşteri ara')).toBeVisible();
  });

  test('Görüşme geçmişinde arama kutusu var', async ({ page }) => {
    await page.goto('/gorusme-gecmisi');
    await expect(page.getByLabel('Görüşme ara')).toBeVisible();
  });

  test('Personel yönetiminde arama kutusu var', async ({ page }) => {
    await page.goto('/personel-yonetimi');
    await expect(page.getByLabel('Personel ara')).toBeVisible();
  });
});
