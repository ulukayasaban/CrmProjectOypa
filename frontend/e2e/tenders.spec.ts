import { test, expect } from './fixtures';

/** İhale listesi: arama + segment + sayfalama kontrolleri. */
test.describe('İhaleler', () => {
  test('arama sonucu filtreler ve temizleyince geri gelir', async ({ page }) => {
    await page.goto('/tenders/aktif');

    // Seed: "Global Enerji ... İhalesi 2026" aktif segmentte
    await expect(page.getByText('Global Enerji', { exact: false })).toBeVisible();

    const search = page.getByRole('searchbox', { name: 'İhale ara' });
    await search.fill('zzz-eslesmeyen-terim');
    // Arama sonucu boşsa standart "... için sonuç bulunamadı." mesajı gösterilir.
    await expect(page.getByText('için sonuç bulunamadı', { exact: false })).toBeVisible();

    await search.fill('');
    await expect(page.getByText('Global Enerji', { exact: false })).toBeVisible();
  });

  test('sektör filtresi uygulanır', async ({ page }) => {
    await page.goto('/tenders/aktif');
    await page.getByLabel('İş koluna göre filtrele').selectOption({ label: 'Enerji' });
    // Enerji seed ihalesi görünür kalır
    await expect(page.getByText('Global Enerji', { exact: false })).toBeVisible();
  });

  test('kazanılan segmentinde kazanılan ihale görünür', async ({ page }) => {
    await page.goto('/tenders/kazanilan');
    // Seed: "AVM Tesis Yönetimi Güvenlik Hizmetleri" kazanıldı
    await expect(page.getByText('AVM Tesis', { exact: false })).toBeVisible();
  });
});
