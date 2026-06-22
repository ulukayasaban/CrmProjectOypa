import { z } from 'zod';

const methodValues = ['Visit', 'Phone', 'Email'] as const;

export const meetingSchema = z.object({
  companyId: z.string().min(1, 'Firma seçiniz.'),
  contactId: z.string().optional().or(z.literal('')),
  salesRepId: z.string().min(1, 'Temsilci seçiniz.'),
  date: z.string().min(1, 'Tarih gereklidir.'),
  time: z.string().min(1, 'Saat gereklidir.'),
  address: z.string().min(1, 'Adres gereklidir.'),
  method: z.enum(methodValues, { message: 'Yöntem seçiniz.' }),
});

export type MeetingFormValues = z.infer<typeof meetingSchema>;

export const noteSchema = z.object({
  content: z
    .string()
    .min(1, 'Not içeriği boş olamaz.')
    .max(2000, 'Not en fazla 2000 karakter olabilir.'),
});

export type NoteFormValues = z.infer<typeof noteSchema>;
