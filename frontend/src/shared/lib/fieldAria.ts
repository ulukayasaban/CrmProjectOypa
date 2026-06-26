/**
 * Bir input/select/textarea'ya hata durumunda aria-invalid + aria-describedby
 * öznitelikleri üretir. Hata yoksa boş nesne döner (öznitelik eklenmez).
 * describedby id'si `${name}-error` formatındadır; FieldError id'si ile eşleşmeli.
 */
export function fieldAria(
  name: string,
  hasError: boolean,
): { 'aria-invalid': true; 'aria-describedby': string } | Record<string, never> {
  return hasError
    ? { 'aria-invalid': true, 'aria-describedby': `${name}-error` }
    : {};
}
