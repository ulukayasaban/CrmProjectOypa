import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { FieldError } from '../../../shared/components/FieldError';
import { fieldAria } from '../../../shared/lib/fieldAria';
import { useToast } from '../../../shared/components/toast/ToastProvider';
import { applyServerFieldErrors } from '../../../shared/lib/applyServerFieldErrors';
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
  const toast = useToast();

  const isEdit = Boolean(goal);

  const {
    register,
    handleSubmit,
    setError,
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
    try {
      if (isEdit && goal) {
        await updateGoal.mutateAsync({ id: goal.id, payload });
        toast.success('Hedef güncellendi.');
      } else {
        await createGoal.mutateAsync(payload);
        toast.success('Hedef oluşturuldu.');
      }
      onClose();
    } catch (err) {
      const generalMsg = applyServerFieldErrors<GoalFormValues>(err, setError);
      if (generalMsg) {
        toast.error(generalMsg);
      }
    }
  });

  return (
    <Modal title={isEdit ? 'Hedef Düzenle' : 'Yeni Hedef'} onClose={onClose}>
      <form className="crm-form" onSubmit={onSubmit}>
        <div className="form-group">
          <label htmlFor="assigneeEmployeeId">Atanan Personel</label>
          <select id="assigneeEmployeeId" {...fieldAria('assigneeEmployeeId', !!errors.assigneeEmployeeId)} {...register('assigneeEmployeeId')}>
            <option value="">-- Personel seçin --</option>
            {(managedEmployees.data ?? []).map((emp) => (
              <option key={emp.id} value={emp.id}>
                {emp.fullName ?? emp.title ?? emp.email ?? emp.id}
              </option>
            ))}
          </select>
          <FieldError id="assigneeEmployeeId-error" message={errors.assigneeEmployeeId?.message} />
        </div>

        <div className="form-group">
          <label htmlFor="segment">Segment</label>
          <select id="segment" {...fieldAria('segment', !!errors.segment)} {...register('segment')}>
            {GOAL_SEGMENTS.map((s) => (
              <option key={s} value={s}>
                {SEGMENT_LABELS[s]}
              </option>
            ))}
          </select>
          <FieldError id="segment-error" message={errors.segment?.message} />
        </div>

        <div className="form-group">
          <label htmlFor="weeklyTarget">Haftalık Hedef (Görüşme)</label>
          <input
            id="weeklyTarget"
            type="number"
            min={1}
            {...fieldAria('weeklyTarget', !!errors.weeklyTarget)}
            {...register('weeklyTarget', { valueAsNumber: true })}
          />
          <FieldError id="weeklyTarget-error" message={errors.weeklyTarget?.message} />
        </div>

        <div className="form-group">
          <label htmlFor="title">Başlık (opsiyonel)</label>
          <input id="title" type="text" {...fieldAria('title', !!errors.title)} {...register('title')} />
          <FieldError id="title-error" message={errors.title?.message} />
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
