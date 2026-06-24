import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { USER_ROLE_OPTIONS } from '../../../shared/constants/labels';
import { useToast } from '../../../shared/components/toast/ToastProvider';
import { applyServerFieldErrors } from '../../../shared/lib/applyServerFieldErrors';
import { useRegisterUser } from '../model/useRegisterUser';
import {
  registerSchema,
  type RegisterFormValues,
} from '../model/registerSchema';

interface RegisterUserModalProps {
  onClose: () => void;
}

export function RegisterUserModal({ onClose }: RegisterUserModalProps) {
  const registerUser = useRegisterUser();
  const toast = useToast();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await registerUser.mutateAsync(values);
      toast.success('Kullanıcı başarıyla oluşturuldu.');
      onClose();
    } catch (err) {
      const generalMsg = applyServerFieldErrors<RegisterFormValues>(err, setError);
      if (generalMsg) {
        toast.error(generalMsg);
      }
    }
  });

  return (
    <Modal title="Yeni Kullanıcı" onClose={onClose} width={600}>
      <form className="crm-form" onSubmit={onSubmit}>
        <div className="form-group">
          <label htmlFor="fullName">Ad Soyad</label>
          <input id="fullName" {...register('fullName')} />
          {errors.fullName && (
            <span className="field-error">{errors.fullName.message}</span>
          )}
        </div>
        <div className="form-row">
          <div className="form-group">
            <label htmlFor="email">E-posta</label>
            <input id="email" type="email" {...register('email')} />
            {errors.email && (
              <span className="field-error">{errors.email.message}</span>
            )}
          </div>
          <div className="form-group">
            <label htmlFor="role">Rol</label>
            <select id="role" defaultValue="" {...register('role')}>
              <option value="" disabled>
                Seçiniz
              </option>
              {USER_ROLE_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
            {errors.role && (
              <span className="field-error">{errors.role.message}</span>
            )}
          </div>
        </div>
        <div className="form-group">
          <label htmlFor="password">Parola</label>
          <input id="password" type="password" {...register('password')} />
          {errors.password && (
            <span className="field-error">{errors.password.message}</span>
          )}
        </div>
        <div className="modal-footer">
          <button type="button" className="btn btn-ghost" onClick={onClose}>
            İptal
          </button>
          <button
            type="submit"
            className="btn btn-primary"
            disabled={isSubmitting}
          >
            {isSubmitting ? 'Kaydediliyor...' : 'Kaydet'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
