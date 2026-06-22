import { useState } from 'react';
import { useGoals, useDeleteGoal } from '../features/goals/model/useGoals';
import { GoalFormModal } from '../features/goals/ui/GoalFormModal';
import { GoalWeeksModal } from '../features/goals/ui/GoalWeeksModal';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { PlusIcon } from '../shared/components/icons';
import { getErrorMessage } from '../shared/lib/errorMessage';
import type { GoalDto } from '../entities/goal/model/goal';

const SEGMENT_LABELS: Record<string, string> = {
  Customer: 'Müşteri',
  Lead: 'Potansiyel',
  All: 'Hepsi',
};

export default function GoalsPage() {
  const { data, isLoading, isError, error } = useGoals();
  const deleteGoal = useDeleteGoal();

  const [createModal, setCreateModal] = useState(false);
  const [editGoal, setEditGoal] = useState<GoalDto | null>(null);
  const [weeksGoal, setWeeksGoal] = useState<GoalDto | null>(null);

  if (isLoading) return <Spinner />;
  if (isError) return <StateBlock message={getErrorMessage(error)} />;

  const handleDelete = (goal: GoalDto) => {
    const label = goal.title ?? goal.assigneeName;
    if (window.confirm(`"${label}" hedefini silmek istediğinize emin misiniz?`)) {
      void deleteGoal.mutateAsync(goal.id);
    }
  };

  return (
    <>
      <div className="glass full-width card">
        <div className="card-head">
          <h3>Hedefler</h3>
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={() => setCreateModal(true)}
          >
            <PlusIcon size={14} /> Yeni Hedef
          </button>
        </div>

        <div
          className="data-table-container"
          style={{ background: 'none', border: 'none', marginTop: 15 }}
        >
          <table className="data-table">
            <thead>
              <tr>
                <th>Atanan</th>
                <th>Segment</th>
                <th>Haftalık Hedef</th>
                <th>Bu Hafta</th>
                <th>İşlemler</th>
              </tr>
            </thead>
            <tbody>
              {(!data || data.length === 0) && (
                <tr>
                  <td colSpan={5} className="table-empty">
                    Henüz tanımlanmış hedef yok.
                  </td>
                </tr>
              )}
              {(data ?? []).map((goal) => {
                const pct = Math.min(100, Math.round(goal.currentPercent));
                return (
                  <tr key={goal.id}>
                    <td>{goal.assigneeName ?? 'Atanmamış'}</td>
                    <td>{SEGMENT_LABELS[goal.segment] ?? goal.segment}</td>
                    <td>{goal.weeklyTarget}</td>
                    <td style={{ minWidth: 160 }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                        <span style={{ fontSize: '0.8rem', whiteSpace: 'nowrap' }}>
                          {goal.currentAchieved}/{goal.currentTarget}
                        </span>
                        <div className="progress-track" style={{ flex: 1 }}>
                          <div
                            className="progress-fill"
                            style={{ width: `${pct}%` }}
                          />
                        </div>
                        <span style={{ fontSize: '0.8rem', minWidth: 36 }}>
                          {pct}%
                        </span>
                      </div>
                    </td>
                    <td>
                      <div style={{ display: 'flex', gap: 6 }}>
                        <button
                          type="button"
                          className="btn btn-ghost btn-sm"
                          onClick={() => setEditGoal(goal)}
                        >
                          Düzenle
                        </button>
                        <button
                          type="button"
                          className="btn btn-ghost btn-sm"
                          onClick={() => setWeeksGoal(goal)}
                        >
                          Geçmiş
                        </button>
                        <button
                          type="button"
                          className="btn btn-ghost btn-sm"
                          onClick={() => handleDelete(goal)}
                          disabled={deleteGoal.isPending}
                        >
                          Sil
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {createModal && <GoalFormModal onClose={() => setCreateModal(false)} />}
      {editGoal && (
        <GoalFormModal
          goal={editGoal}
          onClose={() => setEditGoal(null)}
        />
      )}
      {weeksGoal && (
        <GoalWeeksModal
          goal={weeksGoal}
          onClose={() => setWeeksGoal(null)}
        />
      )}
    </>
  );
}
