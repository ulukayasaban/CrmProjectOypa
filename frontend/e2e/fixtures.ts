import { test as base } from '@playwright/test';
import { login } from './helpers';

/**
 * Giriş yapılmış (authenticated) `page` sağlayan fixture.
 * Bu dosyadan `test`/`expect` import eden spec'lerde her test öncesi otomatik giriş yapılır.
 */
export const test = base.extend({
  // İkinci parametre Playwright'ın "use" fonksiyonudur; react-hooks kuralının
  // React `use` hook'uyla karıştırmaması için `runTest` olarak adlandırıldı.
  page: async ({ page }, runTest) => {
    await login(page);
    await runTest(page);
  },
});

export { expect } from '@playwright/test';
