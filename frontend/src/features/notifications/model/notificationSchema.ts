import { z } from 'zod';

export const sendNotificationSchema = z.object({
  targetUnitId: z
    .string()
    .uuid('Geçerli bir birim seçiniz.')
    .min(1, 'Hedef birim seçiniz.'),
  title: z.string().max(200, 'Başlık en fazla 200 karakter olabilir.').optional(),
  message: z
    .string()
    .min(1, 'Mesaj gereklidir.')
    .max(1000, 'Mesaj en fazla 1000 karakter olabilir.'),
});

export type SendNotificationFormValues = z.infer<typeof sendNotificationSchema>;
