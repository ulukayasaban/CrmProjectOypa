import { useMutation, useQueryClient } from '@tanstack/react-query';
import { authApi, type UpdateProfilePayload } from '../api/authApi';
import type { UserDto } from '../../../entities/user/model/user';
import { queryKeys } from '../../../shared/api/queryKeys';

/** Profil güncelleme mutation hook'u — PATCH /auth/me; başarıda me cache'ini geçersiz kılar */
export function useUpdateProfile() {
  const queryClient = useQueryClient();

  return useMutation<UserDto, unknown, UpdateProfilePayload>({
    mutationFn: (payload) => authApi.updateProfile(payload),
    onSuccess: () => {
      // Güncellenmiş profil verisi için me sorgusunu yeniden tetikle
      void queryClient.invalidateQueries({ queryKey: queryKeys.me });
    },
  });
}
