import { useMutation } from '@tanstack/react-query';
import { authApi, type RegisterPayload } from '../api/authApi';

export function useRegisterUser() {
  return useMutation({
    mutationFn: (payload: RegisterPayload) => authApi.register(payload),
  });
}
