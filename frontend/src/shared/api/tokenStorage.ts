import { STORAGE_KEYS } from '../constants/storageKeys';

/**
 * Access token bellekte (module-scoped) tutulur → localStorage/sessionStorage'a
 * yazılmadığından üçüncü-parti script'lerle (XSS) sızdırılamaz.
 *
 * Refresh token artık HttpOnly çerezde (sunucu tarafı) tutulur; JS erişemez.
 * Burada yalnızca hassas olmayan bir "oturum ipucu" bayrağı saklanır: sayfa
 * yenilemede bootstrap'ın gereksiz /auth/refresh çağrısı yapmasını önler.
 */
let _accessToken: string | null = null;

export const tokenStorage = {
  getAccessToken(): string | null {
    return _accessToken;
  },

  /** Access token'ı belleğe yazar ve oturum ipucunu işaretler. */
  setAccessToken(accessToken: string): void {
    _accessToken = accessToken;
    localStorage.setItem(STORAGE_KEYS.sessionHint, '1');
  },

  /** Daha önce giriş yapılmış mı? (refresh denemesi yapmaya değer mi) */
  hasSessionHint(): boolean {
    return localStorage.getItem(STORAGE_KEYS.sessionHint) === '1';
  },

  clear(): void {
    _accessToken = null;
    localStorage.removeItem(STORAGE_KEYS.sessionHint);
  },
};
