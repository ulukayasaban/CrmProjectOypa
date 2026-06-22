import { z } from 'zod';

const roleValues = ['Admin', 'Sales'] as const;

export const registerSchema = z.object({
  fullName: z.string().min(1, 'Ad soyad gereklidir.'),
  email: z
    .string()
    .min(1, 'E-posta gereklidir.')
    .email('Geçerli bir e-posta giriniz.'),
  password: z.string().min(6, 'Parola en az 6 karakter olmalıdır.'),
  role: z.enum(roleValues, { message: 'Rol seçiniz.' }),
});

export type RegisterFormValues = z.infer<typeof registerSchema>;
