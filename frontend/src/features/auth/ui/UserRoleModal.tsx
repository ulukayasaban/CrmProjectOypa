import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { FieldError } from '../../../shared/components/FieldError';
import { fieldAria } from '../../../shared/lib/fieldAria';
import { USER_ROLE_OPTIONS } from '../../../shared/constants/labels';
import { getErrorMessage } from '../../../shared/lib/errorMessage';
import { toUserRole } from '../../../shared/lib/narrowing';
import { useChangeUserRole } from '../model/useUsers';
import { useToast } from '../../../shared/components/toast/ToastProvider';
import { assignRoleSchema, type AssignRoleFormValues } from '../../employees/model/employeeSchema';
import type { UserDto } from '../../../entities/user/model/user';

interface UserRoleModalProps {
  user: UserDto;
  onClose: () => void;
}

/**
 * Sistem kullanıcısının rolünü değiştirme modalı.
 * AssignRoleModal desenini takip eder; useChangeUserRole + /auth/users/{id}/role kullanır.
 */
export function UserRoleModal({ user, onClose }: UserRoleModalProps) {
  const changeRole = useChangeUserRole();
  const toast = useToast();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<AssignRoleFormValues>({
    resolver: zodResolver(assignRoleSchema),
    defaultValues: { role: toUserRole(user.roles[0]) },
  });

  const onSubmit = handleSubmit(async (values) => {
    await changeRole.mutateAsync({ id: user.id, role: values.role });
    toast.success('Rol güncellendi.');
    onClose();
  });

  return (
    <Modal title="Rol Değiştir" onClose={onClose} width={400}>
      <form className="crm-form" onSubmit={onSubmit}>
        {changeRole.isError && (
          <div className="form-error">{getErrorMessage(changeRole.error)}</div>
        )}
        <p className="muted" style={{ fontSize: '0.85rem' }}>
          <strong>{user.fullName || user.email}</strong> için sistem rolünü seçin.
        </p>
        <div className="form-group">
          <label htmlFor="user-role">Rol</label>
          <select
            id="user-role"
            defaultValue={toUserRole(user.roles[0]) ?? ''}
            {...fieldAria('user-role', !!errors.role)}
            {...register('role')}
          >
            <option value="" disabled>
              Seçiniz
            </option>
            {USER_ROLE_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
          <FieldError id="user-role-error" message={errors.role?.message} />
        </div>
        <div className="modal-footer">
          <button type="button" className="btn btn-ghost" onClick={onClose}>
            İptal
          </button>
          <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
            {isSubmitting ? 'Kaydediliyor...' : 'Kaydet'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
