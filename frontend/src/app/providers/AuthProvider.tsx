import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { authApi, type LoginPayload } from '../../features/auth/api/authApi';
import type { UserDto } from '../../entities/user/model/user';
import { tokenStorage } from '../../shared/api/tokenStorage';
import { setSessionExpiredHandler } from '../../shared/api/httpClient';
import { AuthContext, type AuthContextValue } from './authContext';

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<UserDto | null>(null);
  const [isInitializing, setIsInitializing] = useState(true);

  const clearSession = useCallback(() => {
    tokenStorage.clear();
    setUser(null);
  }, []);

  // Allow the http client to drop the session when refresh fails.
  useEffect(() => {
    setSessionExpiredHandler(() => setUser(null));
  }, []);

  // Sayfa yenilemede oturumu geri yükle.
  // Access token yalnızca bellekte olduğundan reload'da kaybolur; refresh token
  // ise HttpOnly çerezdedir (JS göremez). Daha önce giriş yapılmışsa (oturum
  // ipucu) sessizce /auth/refresh çağrılır (çerez otomatik gider) ve yeni access
  // token + kullanıcı alınır. Çerez yoksa/geçersizse oturum düşürülür.
  useEffect(() => {
    let active = true;
    async function bootstrap() {
      if (!tokenStorage.hasSessionHint()) {
        setIsInitializing(false);
        return;
      }
      try {
        const refreshed = await authApi.refresh();
        tokenStorage.setAccessToken(refreshed.accessToken);
        const me = await authApi.me();
        if (active) setUser(me);
      } catch {
        if (active) clearSession();
      } finally {
        if (active) setIsInitializing(false);
      }
    }
    void bootstrap();
    return () => {
      active = false;
    };
  }, [clearSession]);

  const login = useCallback(async (payload: LoginPayload) => {
    const response = await authApi.login(payload);
    tokenStorage.setAccessToken(response.accessToken);
    setUser(response.user);
  }, []);

  const logout = useCallback(async () => {
    try {
      // Çerez otomatik gönderilir; sunucu refresh token'ı iptal eder ve çerezi siler.
      await authApi.logout();
    } catch {
      // Logout hatası yok sayılır; yerel oturum yine de temizlenir.
    }
    clearSession();
  }, [clearSession]);

  const hasRole = useCallback(
    (role: string) => user?.roles.includes(role) ?? false,
    [user],
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      isInitializing,
      login,
      logout,
      hasRole,
    }),
    [user, isInitializing, login, logout, hasRole],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
