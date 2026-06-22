import { z } from 'zod';

export const salesRepSchema = z.object({
  name: z.string().min(1, 'İsim gereklidir.'),
  email: z
    .string()
    .min(1, 'E-posta gereklidir.')
    .email('Geçerli bir e-posta giriniz.'),
  employeeId: z.string().optional(),
});

export type SalesRepFormValues = z.infer<typeof salesRepSchema>;
