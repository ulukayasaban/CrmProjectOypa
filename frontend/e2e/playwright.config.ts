import { defineConfig, devices } from '@playwright/test';

/**
 * OYPA CRM E2E yapılandırması.
 * Gereksinim: API (5022) ve frontend dev server (5173) ayakta olmalı.
 * Kurulum için bkz. e2e/README.md (Playwright package.json'a eklenmemiştir).
 */
export default defineConfig({
  testDir: '.',
  timeout: 30_000,
  fullyParallel: true,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
});
