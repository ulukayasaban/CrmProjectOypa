import { createContext } from 'react';
import type { UserDto } from '../../entities/user/model/user';
import type { LoginPayload } from '../../features/auth/api/authApi';

export interface AuthContextValue {
  user: UserDto | null;
  isAuthenticated: boolean;
  isInitializing: boolean;
  login: (payload: LoginPayload) => Promise<void>;
  logout: () => Promise<void>;
  hasRole: (role: string) => boolean;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
