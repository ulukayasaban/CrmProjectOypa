export const STORAGE_KEYS = {
  // Token'lar localStorage'da TUTULMAZ:
  //  - access token: yalnızca bellekte (in-memory)
  //  - refresh token: HttpOnly çerez (sunucu yönetir; JS erişemez → XSS koruması)
  // Yalnızca hassas olmayan bir "oturum ipucu" tutulur; sayfa yenilemede
  // gereksiz /auth/refresh çağrısını önlemek için (anonim kullanıcıda atlanır).
  sessionHint: 'oypa_session',
} as const;
