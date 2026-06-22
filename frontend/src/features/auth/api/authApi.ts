import { httpClient } from '../../../shared/api/httpClient';
import type { AuthResponse, UserDto } from '../../../entities/user/model/user';

export interface LoginPayload {
  email: string;
  password: string;
}

export interface RegisterPayload {
  email: string;
  password: string;
  fullName: string;
  role: string;
}

/** Minimal shape returned by /auth/refresh (same envelope as login but user field optional). */
export interface RefreshResponse {
  accessToken: string;
  refreshToken: string;
}

export const authApi = {
  async login(payload: LoginPayload): Promise<AuthResponse> {
    const { data } = await httpClient.post<AuthResponse>(
      '/auth/login',
      payload,
    );
    return data;
  },
  async refresh(refreshToken: string): Promise<RefreshResponse> {
    const { data } = await httpClient.post<RefreshResponse>(
      '/auth/refresh',
      { refreshToken },
    );
    return data;
  },
  async logout(refreshToken: string): Promise<void> {
    await httpClient.post('/auth/logout', { refreshToken });
  },
  async me(): Promise<UserDto> {
    const { data } = await httpClient.get<UserDto>('/auth/me');
    return data;
  },
  async register(payload: RegisterPayload): Promise<void> {
    await httpClient.post('/auth/register', payload);
  },
};
