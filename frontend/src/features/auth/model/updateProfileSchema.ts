import { z } from 'zod';

/** Profil düzenleme formu zod şeması — tüm alanlar opsiyonel, boş string kabul edilmez */
export const updateProfileSchema = z.object({
  fullName: z
    .string()
    .min(1, 'Ad Soyad gereklidir.')
    .max(100, 'Ad Soyad en fazla 100 karakter olabilir.'),
  phone: z
    .string()
    .max(20, 'Telefon numarası en fazla 20 karakter olabilir.')
    .optional()
    .or(z.literal('')),
  position: z
    .string()
    .max(100, 'Pozisyon en fazla 100 karakter olabilir.')
    .optional()
    .or(z.literal('')),
});

export type UpdateProfileFormValues = z.infer<typeof updateProfileSchema>;
