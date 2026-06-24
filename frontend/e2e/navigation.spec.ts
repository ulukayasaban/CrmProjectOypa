import { test, expect } from '@playwright/test';
import { login, expectHeaderTitle } from './helpers';

/** Tüm ana sayfaların yüklendiğini ve başlığının göründüğünü doğrular. */
test.describe('Navigasyon — tüm sayfalar yüklenir', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  const pages: { path: string; title: string }[] = [
    { path: '/', title: 'Genel Bakış' },
    { path: '/leads', title: 'Potansiyel Müşteriler' },
    { path: '/customers/aktif', title: 'Aktif Müşteriler' },
    { path: '/customers/pasif', title: 'Pasif Müşteriler' },
    { path: '/tenders/aktif', title: 'Aktif İhaleler' },
    { path: '/tenders/kazanilan', title: 'Kazanılan İhaleler' },
    { path: '/tenders/kaybedilen', title: 'Kaybedilen İhaleler' },
    { path: '/calendar', title: 'Ziyaret Takvimi' },
    { path: '/gorusme-gecmisi', title: 'Görüşme Geçmişi' },
    { path: '/mail-drafts', title: 'Mail Taslakları' },
    { path: '/raporlar', title: 'Raporlar' },
    { path: '/management', title: 'Yönetim' },
    { path: '/organizasyon', title: 'Organizasyon' },
    { path: '/personel-yonetimi', title: 'Personel Yönetimi' },
    { path: '/hedefler', title: 'Hedefler' },
    { path: '/profile', title: 'Profilim' },
  ];

  for (const { path, title } of pages) {
    test(`${path} → "${title}"`, async ({ page }) => {
      await page.goto(path);
      await expect(page).toHaveURL(new RegExp(path.replace(/\//g, '\\/') + '$'));
      await expectHeaderTitle(page, title);
      // Hata sınırı tetiklenmemeli
      await expect(page.getByText('Bir şeyler ters gitti')).toHaveCount(0);
    });
  }

  test('sidebar linkleri çalışır (Potansiyel Müşteriler)', async ({ page }) => {
    await page.getByRole('link', { name: 'Potansiyel Müşteriler' }).click();
    await expect(page).toHaveURL(/\/leads$/);
  });

  test('sidebar İhaleler grubu genişler ve alt-linke gider', async ({ page }) => {
    await page.getByRole('button', { name: 'İhaleler' }).click();
    await page.getByRole('link', { name: 'Aktif İhaleler' }).click();
    await expect(page).toHaveURL(/\/tenders\/aktif$/);
  });
});
