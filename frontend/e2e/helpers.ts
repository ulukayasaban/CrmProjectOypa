import { type Page, expect } from '@playwright/test';

/** Seed Admin kullanıcısı (DbSeeder org hesabı). */
export const ADMIN = {
  email: 'umur.kutlu@oypa.com.tr',
  password: 'Oypa!2026',
};

/** Giriş yapar ve dashboard'a yönlendiğini doğrular. */
export async function login(
  page: Page,
  email = ADMIN.email,
  password = ADMIN.password,
): Promise<void> {
  await page.goto('/login');
  await page.getByLabel('E-posta').fill(email);
  await page.getByLabel('Parola').fill(password);
  await page.getByRole('button', { name: 'Giriş Yap' }).click();
  await expect(page).toHaveURL('http://localhost:5173/');
  // Dashboard yüklendi
  await expect(page.getByText('Aktif Leadler')).toBeVisible();
}

/** Header'daki sayfa başlığını (h2) doğrular. Aynı metin sayfada birden çok olabilir → first(). */
export async function expectHeaderTitle(page: Page, title: string): Promise<void> {
  await expect(page.getByRole('heading', { name: title }).first()).toBeVisible();
}
