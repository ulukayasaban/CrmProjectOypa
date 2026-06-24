import { z } from 'zod';

/** Parola Değiştirme formu zod şeması — mevcut parola + yeni parola (min 8) + tekrar eşleşmeli */
export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Mevcut parola gereklidir.'),
    newPassword: z
      .string()
      .min(8, 'Yeni parola en az 8 karakter olmalıdır.'),
    confirmPassword: z.string().min(1, 'Yeni parolayı tekrar giriniz.'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Yeni parolalar eşleşmiyor.',
    path: ['confirmPassword'],
  });

export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>;
