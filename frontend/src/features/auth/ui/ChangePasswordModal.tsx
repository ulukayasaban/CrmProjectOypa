import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { useToast } from '../../../shared/components/toast/ToastProvider';
import { applyServerFieldErrors } from '../../../shared/lib/applyServerFieldErrors';
import {
  changePasswordSchema,
  type ChangePasswordFormValues,
} from '../model/changePasswordSchema';
import { useChangePassword } from '../model/useChangePassword';

interface ChangePasswordModalProps {
  onClose: () => void;
}

/**
 * Parola Değiştirme modalı — giriş yapmış kullanıcıya özel.
 * POST /auth/change-password; fieldErrors RHF ile alanlara uygulanır.
 */
export function ChangePasswordModal({ onClose }: ChangePasswordModalProps) {
  const toast = useToast();
  const { mutateAsync, isPending } = useChangePassword();

  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors },
  } = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutateAsync({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      });
      toast.success('Parola başarıyla değiştirildi.');
      reset();
      onClose();
    } catch (error) {
      // Backend 400 fieldErrors'ını alanlara uygula; genel hata toast'a düşer
      const generalMessage = applyServerFieldErrors(error, setError);
      if (generalMessage) {
        toast.error(generalMessage);
      }
    }
  });

  return (
    <Modal title="Parolayı Değiştir" onClose={onClose} width={440}>
      <form className="crm-form" onSubmit={onSubmit}>
        {/* Mevcut parola */}
        <div className="form-group">
          <label htmlFor="cp-current">Mevcut Parola</label>
          <input
            id="cp-current"
            type="password"
            autoComplete="current-password"
            {...register('currentPassword')}
          />
          {errors.currentPassword && (
            <span className="field-error">{errors.currentPassword.message}</span>
          )}
        </div>

        {/* Yeni parola */}
        <div className="form-group">
          <label htmlFor="cp-new">Yeni Parola</label>
          <input
            id="cp-new"
            type="password"
            autoComplete="new-password"
            placeholder="En az 8 karakter"
            {...register('newPassword')}
          />
          {errors.newPassword && (
            <span className="field-error">{errors.newPassword.message}</span>
          )}
        </div>

        {/* Yeni parola tekrar */}
        <div className="form-group">
          <label htmlFor="cp-confirm">Yeni Parola (Tekrar)</label>
          <input
            id="cp-confirm"
            type="password"
            autoComplete="new-password"
            {...register('confirmPassword')}
          />
          {errors.confirmPassword && (
            <span className="field-error">{errors.confirmPassword.message}</span>
          )}
        </div>

        <div style={{ display: 'flex', gap: '0.5rem', marginTop: '1rem' }}>
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={onClose}
            disabled={isPending}
          >
            Vazgeç
          </button>
          <button
            type="submit"
            className="btn btn-primary"
            style={{ flex: 1 }}
            disabled={isPending}
          >
            {isPending ? 'Değiştiriliyor...' : 'Parolayı Değiştir'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
