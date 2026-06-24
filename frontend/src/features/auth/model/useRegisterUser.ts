import { useMutation, useQueryClient } from '@tanstack/react-query';
import { authApi, type RegisterPayload } from '../api/authApi';
import { queryKeys } from '../../../shared/api/queryKeys';

/**
 * Yeni kullanıcı kayıt mutasyonu.
 * Başarı durumunda admin kullanıcı listesi (queryKeys.users) yenilenir
 * böylece ManagementPage tablosu otomatik güncellenir.
 */
export function useRegisterUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: RegisterPayload) => authApi.register(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.users });
    },
  });
}
