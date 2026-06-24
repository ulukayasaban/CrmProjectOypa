import { defineConfig, devices } from '@playwright/test';

/**
 * OYPA CRM E2E yapılandırması.
 * webServer: API (5022) ve frontend dev (5173) zaten çalışıyorsa yeniden kullanılır,
 * çalışmıyorsa başlatılır (reuseExistingServer). Kurulum için bkz. e2e/README.md.
 */
export default defineConfig({
  testDir: '.',
  timeout: 30_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  workers: 1,
  retries: 1,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    actionTimeout: 10_000,
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'dotnet run --launch-profile http',
      cwd: '../../backend/src/Api',
      url: 'http://localhost:5022/swagger/v1/swagger.json',
      reuseExistingServer: true,
      timeout: 180_000,
      stdout: 'ignore',
      stderr: 'pipe',
    },
    {
      command: 'npm run dev',
      cwd: '..',
      url: 'http://localhost:5173',
      reuseExistingServer: true,
      timeout: 120_000,
    },
  ],
});
