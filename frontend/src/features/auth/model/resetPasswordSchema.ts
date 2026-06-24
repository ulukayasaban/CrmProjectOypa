import { z } from 'zod';

/** Parola Sıfırlama formu zod şeması — yeni parola en az 8 karakter, tekrar eşleşmeli */
export const resetPasswordSchema = z
  .object({
    newPassword: z
      .string()
      .min(8, 'Parola en az 8 karakter olmalıdır.'),
    confirmPassword: z.string().min(1, 'Parolayı tekrar giriniz.'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Parolalar eşleşmiyor.',
    path: ['confirmPassword'],
  });

export type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>;
