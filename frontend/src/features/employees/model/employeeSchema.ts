import { z } from 'zod';

const roleValues = ['Admin', 'Sales'] as const;

export const createEmployeeSchema = z
  .object({
    title: z.string().min(1, 'Ünvan gereklidir.'),
    fullName: z.string().optional(),
    email: z.string().optional(),
    managerId: z.string().optional(),
    createAccount: z.boolean(),
    role: z.enum(roleValues).optional(),
  })
  .superRefine((values, ctx) => {
    if (values.createAccount) {
      if (!values.email || values.email.trim() === '') {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Hesap oluşturmak için e-posta gereklidir.',
          path: ['email'],
        });
      } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email)) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Geçerli bir e-posta giriniz.',
          path: ['email'],
        });
      }
      if (!values.role) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Hesap oluşturmak için rol seçiniz.',
          path: ['role'],
        });
      }
    }
  });

export type CreateEmployeeFormValues = z.infer<typeof createEmployeeSchema>;

export const updateEmployeeSchema = z.object({
  title: z.string().min(1, 'Ünvan gereklidir.'),
  fullName: z.string().optional(),
  email: z.string().optional(),
});

export type UpdateEmployeeFormValues = z.infer<typeof updateEmployeeSchema>;

export const assignRoleSchema = z.object({
  role: z.enum(roleValues, { message: 'Rol seçiniz.' }),
});

export type AssignRoleFormValues = z.infer<typeof assignRoleSchema>;
