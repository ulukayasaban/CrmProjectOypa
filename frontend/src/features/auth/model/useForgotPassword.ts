import { useMutation } from '@tanstack/react-query';
import { authApi, type ForgotPasswordPayload } from '../api/authApi';

/** Şifremi Unuttum mutation hook'u — POST /auth/forgot-password */
export function useForgotPassword() {
  return useMutation<void, unknown, ForgotPasswordPayload>({
    mutationFn: (payload) => authApi.forgotPassword(payload),
  });
}
