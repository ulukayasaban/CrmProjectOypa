import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { USER_ROLE_OPTIONS } from '../../../shared/constants/labels';
import { getErrorMessage } from '../../../shared/lib/errorMessage';
import { useAssignEmployeeRole } from '../model/useEmployees';
import { assignRoleSchema, type AssignRoleFormValues } from '../model/employeeSchema';
import type { EmployeeDto } from '../../../entities/employee/model/employee';

interface AssignRoleModalProps {
  employee: EmployeeDto;
  onClose: () => void;
}

export function AssignRoleModal({ employee, onClose }: AssignRoleModalProps) {
  const assignRole = useAssignEmployeeRole();
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<AssignRoleFormValues>({
    resolver: zodResolver(assignRoleSchema),
    defaultValues: { role: (employee.role as AssignRoleFormValues['role']) ?? undefined },
  });

  const onSubmit = handleSubmit(async (values) => {
    await assignRole.mutateAsync({ id: employee.id, payload: { role: values.role } });
    onClose();
  });

  return (
    <Modal title="Rol Ata" onClose={onClose} width={400}>
      <form className="crm-form" onSubmit={onSubmit}>
        {assignRole.isError && (
          <div className="form-error">{getErrorMessage(assignRole.error)}</div>
        )}
        <p className="muted" style={{ fontSize: '0.85rem' }}>
          <strong>{employee.title}{employee.fullName ? ` — ${employee.fullName}` : ''}</strong>{' '}
          için sistem rolünü seçin.
        </p>
        <div className="form-group">
          <label htmlFor="role">Rol</label>
          <select id="role" defaultValue={employee.role ?? ''} {...register('role')}>
            <option value="" disabled>Seçiniz</option>
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
