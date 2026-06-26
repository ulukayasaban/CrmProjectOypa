export interface UserDto {
  id: string;
  email: string;
  fullName: string;
  position: string | null;
  phone: string | null;
  roles: string[];
}

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  /**
   * Artık kullanılmıyor: refresh token HttpOnly çerezde döner ve gövdede boş gelir.
   * Geriye dönük uyumluluk için opsiyonel tutuluyor.
   */
  refreshToken?: string;
  user: UserDto;
}
