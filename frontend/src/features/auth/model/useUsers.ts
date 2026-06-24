import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import { userManagementApi } from '../api/userManagementApi';

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
