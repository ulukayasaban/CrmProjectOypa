import { z } from 'zod';

const sectorValues = [
  'Tourism',
  'Retail',
  'FacilityManagement',
  'Energy',
  'Other',
] as const;

const tenderStatusValues = [
  'Hazirlik',
  'TeklifVerildi',
  'Kazanildi',
  'Kaybedildi',
  'Iptal',
] as const;

export const tenderSchema = z.object({
  companyId: z.string().min(1, 'Firma seçiniz.'),
  title: z.string().min(1, 'İhale başlığı gereklidir.'),
  tenderNumber: z.string().optional().or(z.literal('')),
  sector: z.enum(sectorValues, { message: 'İş kolu seçiniz.' }),
  tenderDate: z.string().min(1, 'İhale tarihi gereklidir.'),
  status: z.enum(tenderStatusValues).optional(),
  // Boş number input'ları formda `null`'a çevrilir (setValueAs), böylece
  // `valueAsNumber`'ın ürettiği NaN doğrulama hatası oluşmaz.
  personnelCount: z
    .number()
    .int()
    .min(0, 'Personel sayısı 0 veya daha büyük olmalıdır.')
    .nullable()
    .optional(),
  estimatedValue: z
    .number()
    .min(0, 'Tahmini değer 0 veya daha büyük olmalıdır.')
    .nullable()
    .optional(),
  volume: z
    .number()
    .min(0, 'Hacim 0 veya daha büyük olmalıdır.')
    .nullable()
    .optional(),
  quantity: z
    .number()
    .int()
    .min(0, 'Miktar 0 veya daha büyük olmalıdır.')
    .nullable()
    .optional(),
  description: z.string().optional().or(z.literal('')),
  assignedSalesRepId: z.string().optional().or(z.literal('')),
});

export type TenderFormValues = z.infer<typeof tenderSchema>;
