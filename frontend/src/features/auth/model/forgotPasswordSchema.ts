import { z } from 'zod';

/** Şifremi Unuttum formu zod şeması */
export const forgotPasswordSchema = z.object({
  email: z
    .string()
    .min(1, 'E-posta gereklidir.')
    .email('Geçerli bir e-posta giriniz.'),
});

export type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>;
