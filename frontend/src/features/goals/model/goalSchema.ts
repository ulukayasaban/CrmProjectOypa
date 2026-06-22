import { z } from 'zod';

export const GOAL_SEGMENTS = ['Customer', 'Lead', 'All'] as const;

export const goalSchema = z.object({
  assigneeEmployeeId: z
    .string()
    .uuid('Geçerli bir personel seçiniz.')
    .min(1, 'Atanan personel zorunludur.'),
  segment: z.enum(GOAL_SEGMENTS, {
    error: 'Geçerli bir segment seçiniz.',
  }),
  weeklyTarget: z
    .number({ message: 'Geçerli bir sayı giriniz.' })
    .int('Tam sayı giriniz.')
    .min(1, 'En az 1 olmalıdır.'),
  title: z.string().optional(),
});

export type GoalFormValues = z.infer<typeof goalSchema>;
