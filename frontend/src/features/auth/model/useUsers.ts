import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import { userManagementApi } from '../api/userManagementApi';
import type { UserRole } from '../../../shared/types/enums';

/**
 * Tüm sistem kullanıcılarını listeler.
 * Yalnızca Admin rolündeki sayfalarda çağrılmalıdır.
 */
export function useUsers() {
  return useQuery({
    queryKey: queryKeys.users,
    queryFn: userManagementApi.getAll,
  });
}

/**
 * Kullanıcı silme mutasyonu.
 * Başarı durumunda kullanıcı listesi (queryKeys.users) yenilenir.
 * Kendini silme girişimi backend tarafından 400 hatasıyla engellenir
 * ve çağıran bileşen hatayı toast olarak gösterir.
 */
export function useDeleteUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => userManagementApi.deleteUser(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.users });
    },
  });
}

/**
 * Kullanıcı rolü değiştirme mutasyonu.
 * Başarıda kullanıcı listesi yenilenir.
 * Admin kendi rolünü değiştiremez — UI'da gizlenir, backend 403 döner.
 */
export function useChangeUserRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, role }: { id: string; role: UserRole }) =>
      userManagementApi.changeRole(id, role),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.users });
    },
  });
}

/**
 * Kullanıcı parolası sıfırlama mutasyonu.
 * Başarıda { email, tempPassword } döner; çağıran bileşen AccountCredentialDialog'u açar.
 * Kullanıcı listesi yenilenmez (parola sıfırlama listeyi etkilemez).
 */
export function useResetUserPassword() {
  return useMutation({
    mutationFn: (id: string) => userManagementApi.resetPassword(id),
  });
}
