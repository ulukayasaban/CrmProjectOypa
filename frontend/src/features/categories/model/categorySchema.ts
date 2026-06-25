import { z } from 'zod';

export const categorySchema = z.object({
  name: z
    .string()
    .min(1, 'Kategori adı gereklidir.')
    .max(80, 'Kategori adı en fazla 80 karakter olabilir.'),
  color: z
    .string()
    .regex(/^#([0-9A-Fa-f]{6})$/, 'Geçerli bir hex renk kodu giriniz (örn. #D4AF37).'),
});

export type CategoryFormValues = z.infer<typeof categorySchema>;
