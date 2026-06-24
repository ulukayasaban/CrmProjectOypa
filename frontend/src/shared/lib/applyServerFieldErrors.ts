/**
 * Backend 400 yanıtındaki fieldErrors'ı RHF setError'a uygular.
 * Alan eşleşmesi yoksa / fieldErrors boşsa genel hata mesajını döndürür.
 * Dönen string null → tüm hatalar alanlara uygulandı (ek toast gerekmez).
 * Dönen string → toast.error için genel hata mesajı.
 */
import type { FieldValues, Path, UseFormSetError } from 'react-hook-form';
import axios from 'axios';
import { getErrorMessage } from './errorMessage';

/** Backend hata zarfı — fieldErrors opsiyonel */
interface ErrorEnvelope {
  success: false;
  message: string;
  errors: string[];
  fieldErrors?: Record<string, string[]>;
}

function isErrorEnvelope(value: unknown): value is ErrorEnvelope {
  return (
    typeof value === 'object' &&
    value !== null &&
    'success' in value &&
    (value as Record<string, unknown>)['success'] === false
  );
}

/**
 * @param error  - mutation onError'dan gelen hata nesnesi
 * @param setError - useForm().setError
 * @returns      - null: tüm hatalar alanlara uygulandı (toast gerekmez)
 *                 string: toast.error için genel mesaj
 */
export function applyServerFieldErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
): string | null {
  // Axios HTTP hatası mı?
  if (!axios.isAxiosError(error)) {
    return getErrorMessage(error);
  }

  const data: unknown = error.response?.data;

  if (!isErrorEnvelope(data)) {
    return getErrorMessage(error);
  }

  const { fieldErrors } = data;

  // fieldErrors yoksa / boşsa → genel mesaj döndür
  if (!fieldErrors || Object.keys(fieldErrors).length === 0) {
    return getErrorMessage(error);
  }

  // Alan hatalarını RHF'e uygula; anahtarlar camelCase (backend sözleşmesi)
  let hasFieldMatch = false;
  for (const [field, messages] of Object.entries(fieldErrors)) {
    const message = messages.join(' ');
    // Path<T> cast: runtime'da backend'den gelen alan adı — strict modda güvenli
    setError(field as Path<T>, { type: 'server', message });
    hasFieldMatch = true;
  }

  // Tüm hatalar alanlara uygulandıysa genel toast gerekmez
  if (hasFieldMatch) return null;

  return getErrorMessage(error);
}
