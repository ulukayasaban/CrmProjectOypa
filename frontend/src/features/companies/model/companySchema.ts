import { z } from 'zod';

const sectorValues = [
  'Tourism',
  'Retail',
  'FacilityManagement',
  'Energy',
  'Other',
] as const;

const sourceValues = [
  'Referral',
  'Website',
  'Fair',
  'ColdCall',
  'Other',
] as const;

export const companySchema = z.object({
  title: z.string().min(1, 'Firma ünvanı gereklidir.'),
  sector: z.enum(sectorValues, { message: 'Sektör seçiniz.' }),
  phone: z.string().min(1, 'Telefon gereklidir.'),
  email: z
    .string()
    .min(1, 'E-posta gereklidir.')
    .email('Geçerli bir e-posta giriniz.'),
  address: z.string().min(1, 'Adres gereklidir.'),
  city: z.string().optional().or(z.literal('')),
  website: z.string().optional().or(z.literal('')),
  taxNumber: z.string().optional().or(z.literal('')),
  source: z.enum(sourceValues).optional().or(z.literal('')),
});

export type CompanyFormValues = z.infer<typeof companySchema>;

export const contactSchema = z.object({
  name: z.string().min(1, 'İsim gereklidir.'),
  email: z
    .string()
    .email('Geçerli bir e-posta giriniz.')
    .optional()
    .or(z.literal('')),
  phone: z.string().optional().or(z.literal('')),
});

export type ContactFormValues = z.infer<typeof contactSchema>;
