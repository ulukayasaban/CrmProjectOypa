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

  // Restore the session on a hard refresh.
  // Access token lives only in memory and is lost on reload, so we use the
  // persisted refresh token to silently obtain a new access token, then
  // fetch the current user.  If the refresh token is absent or invalid we
  // drop the session and redirect to login (via ProtectedRoute).
  useEffect(() => {
    let active = true;
    async function bootstrap() {
      if (!tokenStorage.getRefreshToken()) {
        setIsInitializing(false);
        return;
      }
      try {
        const refreshed = await authApi.refresh(tokenStorage.getRefreshToken()!);
        tokenStorage.setTokens(refreshed.accessToken, refreshed.refreshToken);
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
    tokenStorage.setTokens(response.accessToken, response.refreshToken);
    setUser(response.user);
  }, []);

  const logout = useCallback(async () => {
    const refreshToken = tokenStorage.getRefreshToken();
    if (refreshToken) {
      try {
        await authApi.logout(refreshToken);
      } catch {
        // Ignore logout failures; the local session is cleared regardless.
      }
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
