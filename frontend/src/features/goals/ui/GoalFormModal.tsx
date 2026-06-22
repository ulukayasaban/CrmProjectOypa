import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { getErrorMessage } from '../../../shared/lib/errorMessage';
import { useManagedEmployees } from '../../employees/model/useEmployees';
import { useCreateGoal, useUpdateGoal } from '../model/useGoals';
import {
  goalSchema,
  GOAL_SEGMENTS,
  type GoalFormValues,
} from '../model/goalSchema';
import type { GoalDto } from '../../../entities/goal/model/goal';

const SEGMENT_LABELS: Record<(typeof GOAL_SEGMENTS)[number], string> = {
  Customer: 'Müşteri',
  Lead: 'Potansiyel',
  All: 'Hepsi',
};

interface GoalFormModalProps {
  goal?: GoalDto;
  onClose: () => void;
}

export function GoalFormModal({ goal, onClose }: GoalFormModalProps) {
  const managedEmployees = useManagedEmployees();
  const createGoal = useCreateGoal();
  const updateGoal = useUpdateGoal();

  const isEdit = Boolean(goal);
  const mutation = isEdit ? updateGoal : createGoal;

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<GoalFormValues>({
    resolver: zodResolver(goalSchema),
    defaultValues: goal
      ? {
          assigneeEmployeeId: goal.assigneeEmployeeId,
          segment: goal.segment,
          weeklyTarget: goal.weeklyTarget,
          title: goal.title ?? '',
        }
      : { segment: 'All', weeklyTarget: 1, title: '' },
  });

  const onSubmit = handleSubmit(async (values) => {
    const payload = {
      assigneeEmployeeId: values.assigneeEmployeeId,
      segment: values.segment,
      weeklyTarget: values.weeklyTarget,
      title: values.title || undefined,
    };
    if (isEdit && goal) {
      await updateGoal.mutateAsync({ id: goal.id, payload });
    } else {
      await createGoal.mutateAsync(payload);
    }
    onClose();
  });

  return (
    <Modal title={isEdit ? 'Hedef Düzenle' : 'Yeni Hedef'} onClose={onClose}>
      <form className="crm-form" onSubmit={onSubmit}>
        {mutation.isError && (
          <div className="form-error">{getErrorMessage(mutation.error)}</div>
        )}

        <div className="form-group">
          <label htmlFor="assigneeEmployeeId">Atanan Personel</label>
          <select id="assigneeEmployeeId" {...register('assigneeEmployeeId')}>
            <option value="">-- Personel seçin --</option>
            {(managedEmployees.data ?? []).map((emp) => (
              <option key={emp.id} value={emp.id}>
                {emp.fullName ?? emp.title ?? emp.email ?? emp.id}
              </option>
            ))}
          </select>
          {errors.assigneeEmployeeId && (
            <span className="field-error">
              {errors.assigneeEmployeeId.message}
            </span>
          )}
        </div>

        <div className="form-group">
          <label htmlFor="segment">Segment</label>
          <select id="segment" {...register('segment')}>
            {GOAL_SEGMENTS.map((s) => (
              <option key={s} value={s}>
                {SEGMENT_LABELS[s]}
              </option>
            ))}
          </select>
          {errors.segment && (
            <span className="field-error">{errors.segment.message}</span>
          )}
        </div>

        <div className="form-group">
          <label htmlFor="weeklyTarget">Haftalık Hedef (Görüşme)</label>
          <input
            id="weeklyTarget"
            type="number"
            min={1}
            {...register('weeklyTarget', { valueAsNumber: true })}
          />
          {errors.weeklyTarget && (
            <span className="field-error">{errors.weeklyTarget.message}</span>
          )}
        </div>

        <div className="form-group">
          <label htmlFor="title">Başlık (opsiyonel)</label>
          <input id="title" type="text" {...register('title')} />
          {errors.title && (
            <span className="field-error">{errors.title.message}</span>
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
