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

/** POST /auth/change-password — kimlik doğrulama gerektirir */
export interface ChangePasswordPayload {
  currentPassword: string;
  newPassword: string;
}

/** POST /auth/forgot-password — anonim, her durumda 200 döner */
export interface ForgotPasswordPayload {
  email: string;
}

/** POST /auth/reset-password — anonim, token + yeni parola */
export interface ResetPasswordPayload {
  email: string;
  token: string;
  newPassword: string;
}

/** PATCH /auth/me — profil güncelleme; tüm alanlar opsiyonel */
export interface UpdateProfilePayload {
  fullName?: string;
  phone?: string;
  position?: string;
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

  /** Giriş yapmış kullanıcının parolasını değiştirir. */
  async changePassword(payload: ChangePasswordPayload): Promise<void> {
    await httpClient.post('/auth/change-password', payload);
  },

  /**
   * Parola sıfırlama bağlantısı talep eder.
   * Güvenlik gereği backend her zaman 200 döner; bilgi sızdırma önlenir.
   */
  async forgotPassword(payload: ForgotPasswordPayload): Promise<void> {
    await httpClient.post('/auth/forgot-password', payload);
  },

  /** E-posta linki ile gelen token + yeni parola ile sıfırlama yapar. */
  async resetPassword(payload: ResetPasswordPayload): Promise<void> {
    await httpClient.post('/auth/reset-password', payload);
  },

  /** Giriş yapmış kullanıcının profil bilgilerini günceller. */
  async updateProfile(payload: UpdateProfilePayload): Promise<UserDto> {
    const { data } = await httpClient.patch<UserDto>('/auth/me', payload);
    return data;
  },
};
