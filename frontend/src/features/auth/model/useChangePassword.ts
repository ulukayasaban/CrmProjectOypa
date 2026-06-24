import { useMutation } from '@tanstack/react-query';
import { authApi, type ChangePasswordPayload } from '../api/authApi';

/** Parola değiştirme mutation hook'u — POST /auth/change-password */
export function useChangePassword() {
  return useMutation<void, unknown, ChangePasswordPayload>({
    mutationFn: (payload) => authApi.changePassword(payload),
  });
}
