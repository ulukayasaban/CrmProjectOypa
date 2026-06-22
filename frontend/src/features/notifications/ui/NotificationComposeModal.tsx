import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { getErrorMessage } from '../../../shared/lib/errorMessage';
import { useSendNotification } from '../model/useNotifications';
import {
  sendNotificationSchema,
  type SendNotificationFormValues,
} from '../model/notificationSchema';
import { useEmployees } from '../../org/model/useEmployees';
import type { EmployeeDto } from '../../../entities/employee/model/employee';

interface NotificationComposeModalProps {
  onClose: () => void;
}

function employeeLabel(employee: EmployeeDto): string {
  return employee.fullName
    ? `${employee.title} — ${employee.fullName}`
    : employee.title;
}

export function NotificationComposeModal({
  onClose,
}: NotificationComposeModalProps) {
  const employees = useEmployees();
  const sendNotification = useSendNotification();

  const form = useForm<SendNotificationFormValues>({
    resolver: zodResolver(sendNotificationSchema),
    defaultValues: {
      targetUnitId: '',
      title: '',
      message: '',
    },
  });

  const onSubmit = form.handleSubmit(async (values) => {
    await sendNotification.mutateAsync({
      targetUnitId: values.targetUnitId,
      title: values.title || undefined,
      message: values.message,
    });
    onClose();
  });

  return (
    <Modal title="Bildirim Gönder" onClose={onClose} width={520}>
      <form className="crm-form" onSubmit={onSubmit}>
        {sendNotification.isError && (
          <div className="form-error">
            {getErrorMessage(sendNotification.error)}
          </div>
        )}

        <div className="form-group">
          <label htmlFor="compose-unit">Hedef Birim</label>
          <select
            id="compose-unit"
            defaultValue=""
            {...form.register('targetUnitId')}
          >
            <option value="" disabled>
              Birim seçiniz
            </option>
            {(employees.data ?? []).map((emp) => (
              <option key={emp.id} value={emp.id}>
                {employeeLabel(emp)}
              </option>
            ))}
          </select>
          {form.formState.errors.targetUnitId && (
            <span className="field-error">
              {form.formState.errors.targetUnitId.message}
            </span>
          )}
        </div>

        <div className="form-group">
          <label htmlFor="compose-title">
            Başlık <span className="muted">(opsiyonel)</span>
          </label>
          <input
            id="compose-title"
            type="text"
            {...form.register('title')}
            placeholder="Bildirim başlığı"
          />
          {form.formState.errors.title && (
            <span className="field-error">
              {form.formState.errors.title.message}
            </span>
          )}
        </div>

        <div className="form-group">
          <label htmlFor="compose-message">Mesaj</label>
          <textarea
            id="compose-message"
            rows={4}
            {...form.register('message')}
            placeholder="Bildirim mesajını yazınız..."
          />
          {form.formState.errors.message && (
            <span className="field-error">
              {form.formState.errors.message.message}
            </span>
          )}
        </div>

        <div className="modal-footer">
          <button type="button" className="btn btn-ghost" onClick={onClose}>
            İptal
          </button>
          <button
            type="submit"
            className="btn btn-primary"
            disabled={form.formState.isSubmitting}
          >
            {form.formState.isSubmitting ? 'Gönderiliyor...' : 'Gönder'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
