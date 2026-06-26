import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { FieldError } from '../../../shared/components/FieldError';
import { fieldAria } from '../../../shared/lib/fieldAria';
import { getErrorMessage } from '../../../shared/lib/errorMessage';
import { useManagedEmployees } from '../../employees/model/useEmployees';
import { useCreateSalesRep, useLinkEmployee } from '../model/useSalesReps';
import {
  salesRepSchema,
  type SalesRepFormValues,
} from '../model/salesRepSchema';

interface SalesRepFormModalProps {
  onClose: () => void;
}

export function SalesRepFormModal({ onClose }: SalesRepFormModalProps) {
  const createSalesRep = useCreateSalesRep();
  const linkEmployee = useLinkEmployee();
  const managedEmployees = useManagedEmployees();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<SalesRepFormValues>({
    resolver: zodResolver(salesRepSchema),
  });

  const onSubmit = handleSubmit(async (values) => {
    const rep = await createSalesRep.mutateAsync({
      name: values.name,
      email: values.email,
    });
    if (values.employeeId) {
      await linkEmployee.mutateAsync({ id: rep.id, employeeId: values.employeeId });
    }
    onClose();
  });

  const mutationError = createSalesRep.isError
    ? createSalesRep.error
    : linkEmployee.isError
      ? linkEmployee.error
      : null;

  return (
    <Modal title="Yeni Temsilci" onClose={onClose}>
      <form className="crm-form" onSubmit={onSubmit}>
        {mutationError && (
          <div className="form-error">{getErrorMessage(mutationError)}</div>
        )}
        <div className="form-group">
          <label htmlFor="name">İsim</label>
          <input id="name" {...fieldAria('name', !!errors.name)} {...register('name')} />
          <FieldError id="name-error" message={errors.name?.message} />
        </div>
        <div className="form-group">
          <label htmlFor="email">E-posta</label>
          <input id="email" type="email" {...fieldAria('email', !!errors.email)} {...register('email')} />
          <FieldError id="email-error" message={errors.email?.message} />
        </div>
        <div className="form-group">
          <label htmlFor="employeeId">Bağlı Personel (opsiyonel)</label>
          <select id="employeeId" {...fieldAria('employeeId', !!errors.employeeId)} {...register('employeeId')}>
            <option value="">-- Personel seçin --</option>
            {(managedEmployees.data ?? []).map((emp) => (
              <option key={emp.id} value={emp.id}>
                {emp.fullName ?? emp.email ?? emp.id}
              </option>
            ))}
          </select>
          <FieldError id="employeeId-error" message={errors.employeeId?.message} />
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
