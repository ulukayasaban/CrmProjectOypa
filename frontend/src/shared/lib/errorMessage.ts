import { ApiError } from '../types/api';

export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.errors.length > 0) {
      return error.errors.join(' ');
    }
    return error.message;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return 'Beklenmeyen bir hata oluştu.';
}
