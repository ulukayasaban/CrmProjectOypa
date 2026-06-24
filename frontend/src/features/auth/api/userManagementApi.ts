import { httpClient } from '../../../shared/api/httpClient';
import type { UserDto } from '../../../entities/user/model/user';

/**
 * Admin kullanıcı yönetim uçları.
 * GET  /auth/users        — tüm kullanıcıları listeler
 * DELETE /auth/users/{id} — kullanıcıyı siler (admin only; kendini silme backend engelli)
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
};
