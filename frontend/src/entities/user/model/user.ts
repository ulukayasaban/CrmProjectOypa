export interface UserDto {
  id: string;
  email: string;
  fullName: string;
  position: string | null;
  phone: string | null;
  roles: string[];
  /**
   * Kullanıcıya bağlı SalesRep kaydının id'si (Employee→ApplicationUser zincirinden).
   * Yalnızca /auth/me yanıtında dolu gelir; portföy eşleşmesinde kullanılır. Yoksa null.
   */
  assignedSalesRepId?: string | null;
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
