interface FieldErrorProps {
  /** İlgili input'un aria-describedby ile işaret ettiği id (ör. "title-error"). */
  id: string;
  /** Hata mesajı; tanımsızsa hiçbir şey render edilmez. */
  message?: string;
}

/**
 * Form alan hatası. role="alert" ile hata göründüğünde ekran okuyuculara
 * otomatik duyurulur; id, ilgili input'un aria-describedby'si ile eşleşmelidir.
 * fieldAria(name, hasError) (shared/lib/fieldAria) yardımcısıyla birlikte kullanılır.
 */
export function FieldError({ id, message }: FieldErrorProps) {
  if (!message) return null;
  return (
    <span id={id} role="alert" className="field-error">
      {message}
    </span>
  );
}
