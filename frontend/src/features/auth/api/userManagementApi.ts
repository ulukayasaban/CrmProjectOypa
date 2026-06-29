import { httpClient } from '../../../shared/api/httpClient';
import type { UserDto } from '../../../entities/user/model/user';
import type { UserRole } from '../../../shared/types/enums';

export type { AccountCredentials } from '../../employees/api/employeeApi';

/**
 * Admin kullanıcı yönetim uçları.
 * GET    /auth/users              — tüm kullanıcıları listeler
 * DELETE /auth/users/{id}         — kullanıcıyı siler (admin only; kendini silme backend engelli)
 * PUT    /auth/users/{id}/role    — kullanıcı rolünü değiştirir (admin kendi rolünü değiştiremez)
 * POST   /auth/users/{id}/reset-password — geçici parola üretir, döner { email, tempPassword }
 */
export const userManagementApi = {
  /** Tüm sistem kullanıcılarını döndürür (Admin only). */
  async getAll(): Promise<UserDto[]> {
    const { data } = await httpClient.get<UserDto[]>('/auth/users');
    return data;
  },

  /** Belirtilen kullanıcıyı siler. Kendini silme backend tarafında 400 ile reddedilir. */
  async deleteUser(id: string): Promise<void> {
    await httpClient.delete(`/auth/users/${id}`);
  },

  /** Kullanıcının rolünü değiştirir. Admin kendi rolünü değiştiremez (backend 403 döner). */
  async changeRole(id: string, role: UserRole): Promise<void> {
    await httpClient.put(`/auth/users/${id}/role`, { role });
  },

  /**
   * Kullanıcı parolasını sıfırlar ve geçici parola üretir.
   * Dönen { email, tempPassword } yalnızca bir kez gösterilmelidir.
   */
  async resetPassword(id: string): Promise<{ email: string; tempPassword: string }> {
    const { data } = await httpClient.post<{ email: string; tempPassword: string }>(
      `/auth/users/${id}/reset-password`,
    );
    return data;
  },
};
