import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { USER_ROLE_OPTIONS } from '../../../shared/constants/labels';
import { getErrorMessage } from '../../../shared/lib/errorMessage';
import { useCreateEmployee, useUpdateEmployee } from '../model/useEmployees';
import {
  createEmployeeSchema,
  updateEmployeeSchema,
  type CreateEmployeeFormValues,
  type UpdateEmployeeFormValues,
} from '../model/employeeSchema';
import type { EmployeeDto } from '../../../entities/employee/model/employee';
import type { AccountCredentials } from '../api/employeeApi';

interface EmployeeFormModalProps {
  /** When provided the modal operates in edit mode. */
  employee?: EmployeeDto;
  /** List used to populate the manager dropdown. */
  managedList: EmployeeDto[];
  onClose: () => void;
  /** Called after a successful create when account credentials are returned. */
  onCredentials?: (credentials: AccountCredentials) => void;
}

export function EmployeeFormModal({
  employee,
  managedList,
  onClose,
  onCredentials,
}: EmployeeFormModalProps) {
  const isEdit = Boolean(employee);
  const createEmployee = useCreateEmployee();
  const updateEmployee = useUpdateEmployee();

  // Create mode form
  const createForm = useForm<CreateEmployeeFormValues>({
    resolver: zodResolver(createEmployeeSchema),
    defaultValues: {
      title: '',
      fullName: '',
      email: '',
      managerId: '',
      createAccount: false,
      role: undefined,
    },
  });

  // Edit mode form
  const editForm = useForm<UpdateEmployeeFormValues>({
    resolver: zodResolver(updateEmployeeSchema),
    defaultValues: {
      title: employee?.title ?? '',
      fullName: employee?.fullName ?? '',
      email: employee?.email ?? '',
    },
  });

  const createAccount = createForm.watch('createAccount');
  const mutationError = isEdit ? updateEmployee.error : createEmployee.error;
  const mutationIsError = isEdit ? updateEmployee.isError : createEmployee.isError;

  const onSubmitCreate = createForm.handleSubmit(async (values) => {
    const result = await createEmployee.mutateAsync({
      title: values.title,
      fullName: values.fullName || undefined,
      email: values.email || undefined,
      managerId: values.managerId || undefined,
      createAccount: values.createAccount,
      role: values.createAccount ? values.role : undefined,
    });
    if (result.account) {
      onCredentials?.(result.account);
    }
    onClose();
  });

  const onSubmitEdit = editForm.handleSubmit(async (values) => {
    if (!employee) return;
    await updateEmployee.mutateAsync({
      id: employee.id,
      payload: {
        title: values.title,
        fullName: values.fullName || undefined,
        email: values.email || undefined,
      },
    });
    onClose();
  });

  // Exclude the edited employee from the manager dropdown
  const managerCandidates = managedList.filter((e) => e.id !== employee?.id);

  return (
    <Modal
      title={isEdit ? 'Personel Düzenle' : 'Yeni Personel'}
      onClose={onClose}
      width={560}
    >
      {isEdit ? (
        <form className="crm-form" onSubmit={onSubmitEdit}>
          {mutationIsError && (
            <div className="form-error">{getErrorMessage(mutationError)}</div>
          )}
          <div className="form-group">
            <label htmlFor="edit-title">Ünvan</label>
            <input id="edit-title" {...editForm.register('title')} />
            {editForm.formState.errors.title && (
              <span className="field-error">{editForm.formState.errors.title.message}</span>
            )}
          </div>
          <div className="form-row">
            <div className="form-group">
              <label htmlFor="edit-fullName">Ad Soyad</label>
              <input id="edit-fullName" {...editForm.register('fullName')} />
            </div>
            <div className="form-group">
              <label htmlFor="edit-email">E-posta</label>
              <input id="edit-email" type="email" {...editForm.register('email')} />
            </div>
          </div>
          <div className="modal-footer">
            <button type="button" className="btn btn-ghost" onClick={onClose}>
              İptal
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={editForm.formState.isSubmitting}
            >
              {editForm.formState.isSubmitting ? 'Kaydediliyor...' : 'Kaydet'}
            </button>
          </div>
        </form>
      ) : (
        <form className="crm-form" onSubmit={onSubmitCreate}>
          {mutationIsError && (
            <div className="form-error">{getErrorMessage(mutationError)}</div>
          )}
          <div className="form-group">
            <label htmlFor="create-title">Ünvan</label>
            <input id="create-title" {...createForm.register('title')} />
            {createForm.formState.errors.title && (
              <span className="field-error">{createForm.formState.errors.title.message}</span>
            )}
          </div>
          <div className="form-row">
            <div className="form-group">
              <label htmlFor="create-fullName">Ad Soyad <span className="muted">(opsiyonel)</span></label>
              <input id="create-fullName" {...createForm.register('fullName')} />
            </div>
            <div className="form-group">
              <label htmlFor="create-managerId">Yönetici <span className="muted">(opsiyonel)</span></label>
              <select id="create-managerId" defaultValue="" {...createForm.register('managerId')}>
                <option value="">Yok</option>
                {managerCandidates.map((e) => (
                  <option key={e.id} value={e.id}>
                    {e.title}{e.fullName ? ` — ${e.fullName}` : ''}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="form-group">
            <label className="checkbox-inline" style={{ fontWeight: 500 }}>
              <input type="checkbox" {...createForm.register('createAccount')} />
              <span>Hesap oluştur</span>
            </label>
          </div>

          {createAccount && (
            <div className="form-row">
              <div className="form-group">
                <label htmlFor="create-email">E-posta</label>
                <input id="create-email" type="email" {...createForm.register('email')} />
                {createForm.formState.errors.email && (
                  <span className="field-error">{createForm.formState.errors.email.message}</span>
                )}
              </div>
              <div className="form-group">
                <label htmlFor="create-role">Rol</label>
                <select id="create-role" defaultValue="" {...createForm.register('role')}>
                  <option value="" disabled>Seçiniz</option>
                  {USER_ROLE_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                {createForm.formState.errors.role && (
                  <span className="field-error">{createForm.formState.errors.role.message}</span>
                )}
              </div>
            </div>
          )}

          <div className="modal-footer">
            <button type="button" className="btn btn-ghost" onClick={onClose}>
              İptal
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={createForm.formState.isSubmitting}
            >
              {createForm.formState.isSubmitting ? 'Kaydediliyor...' : 'Kaydet'}
            </button>
          </div>
        </form>
      )}
    </Modal>
  );
}
