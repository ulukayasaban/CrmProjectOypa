import { STORAGE_KEYS } from '../constants/storageKeys';

/**
 * Access token is stored in a module-scoped variable (in-memory) so it is
 * never accessible to third-party scripts via localStorage / sessionStorage,
 * reducing the XSS attack surface.
 *
 * Refresh token stays in localStorage so it survives hard page reloads;
 * the auth provider uses it to silently restore the session on startup.
 */
let _accessToken: string | null = null;

export const tokenStorage = {
  getAccessToken(): string | null {
    return _accessToken;
  },

  getRefreshToken(): string | null {
    return localStorage.getItem(STORAGE_KEYS.refreshToken);
  },

  setTokens(accessToken: string, refreshToken: string): void {
    _accessToken = accessToken;
    localStorage.setItem(STORAGE_KEYS.refreshToken, refreshToken);
  },

  setAccessToken(accessToken: string): void {
    _accessToken = accessToken;
  },

  clear(): void {
    _accessToken = null;
    localStorage.removeItem(STORAGE_KEYS.refreshToken);
  },
};
