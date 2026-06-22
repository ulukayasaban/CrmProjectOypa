import { z } from 'zod';

export const loginSchema = z.object({
  email: z
    .string()
    .min(1, 'E-posta gereklidir.')
    .email('Geçerli bir e-posta giriniz.'),
  password: z.string().min(1, 'Parola gereklidir.'),
});

export type LoginFormValues = z.infer<typeof loginSchema>;
