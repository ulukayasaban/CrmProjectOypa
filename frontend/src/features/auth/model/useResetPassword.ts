import { useMutation } from '@tanstack/react-query';
import { authApi, type ResetPasswordPayload } from '../api/authApi';

/** Parola sıfırlama mutation hook'u — POST /auth/reset-password */
export function useResetPassword() {
  return useMutation<void, unknown, ResetPasswordPayload>({
    mutationFn: (payload) => authApi.resetPassword(payload),
  });
}
