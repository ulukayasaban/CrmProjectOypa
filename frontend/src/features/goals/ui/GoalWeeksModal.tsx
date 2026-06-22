import { Modal } from '../../../shared/components/Modal';
import { Spinner } from '../../../shared/components/Spinner';
import { StateBlock } from '../../../shared/components/StateBlock';
import { getErrorMessage } from '../../../shared/lib/errorMessage';
import { useGoalWeeks } from '../model/useGoals';
import type { GoalDto } from '../../../entities/goal/model/goal';

interface GoalWeeksModalProps {
  goal: GoalDto;
  onClose: () => void;
}

export function GoalWeeksModal({ goal, onClose }: GoalWeeksModalProps) {
  const { data, isLoading, isError, error } = useGoalWeeks(goal.id);

  const title = goal.title ?? `${goal.assigneeName} — Geçmiş`;

  return (
    <Modal title={title} onClose={onClose} width={600}>
      {isLoading && <Spinner />}
      {isError && <StateBlock message={getErrorMessage(error)} />}
      {data && (
        <div
          className="data-table-container"
          style={{ background: 'none', border: 'none' }}
        >
          <table className="data-table">
            <thead>
              <tr>
                <th>Hafta Başlangıcı</th>
                <th>Hedef</th>
                <th>Gerçekleşen</th>
                <th>İlerleme</th>
              </tr>
            </thead>
            <tbody>
              {data.length === 0 && (
                <tr>
                  <td colSpan={4} className="table-empty">
                    Henüz haftalık kayıt yok.
                  </td>
                </tr>
              )}
              {data.map((week) => {
                const pct = Math.min(100, Math.round(week.percent));
                return (
                  <tr key={week.weekStart}>
                    <td>{week.weekStart}</td>
                    <td>{week.target}</td>
                    <td>{week.achieved}</td>
                    <td style={{ minWidth: 120 }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
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
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
      <div className="modal-footer">
        <button type="button" className="btn btn-ghost" onClick={onClose}>
          Kapat
        </button>
      </div>
    </Modal>
  );
}
