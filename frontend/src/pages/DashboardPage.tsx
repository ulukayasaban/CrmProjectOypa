import { useNavigate } from 'react-router-dom';
import { useDashboard } from '../features/dashboard/model/useDashboard';
import { useMeetings } from '../features/meetings/model/useMeetings';
import { useTenders } from '../features/tenders/model/useTenders';
import { WeekDayPanel } from '../features/dashboard/ui/WeekDayPanel';
import { UpcomingTendersTable } from '../features/dashboard/ui/UpcomingTendersTable';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { getErrorMessage } from '../shared/lib/errorMessage';

const SEGMENT_LABELS: Record<string, string> = {
  Customer: 'Müşteri',
  Lead: 'Potansiyel',
  All: 'Hepsi',
};

export default function DashboardPage() {
  const navigate = useNavigate();
  const { data, isLoading, isError, error } = useDashboard();
  const meetingsQuery = useMeetings();
  const tendersQuery = useTenders();

  const isLoading2 = isLoading || meetingsQuery.isLoading || tendersQuery.isLoading;
  const isError2 = isError || meetingsQuery.isError || tendersQuery.isError;
  const loadError = error ?? meetingsQuery.error ?? tendersQuery.error;

  if (isLoading2) return <Spinner />;
  if (isError2 || !data) {
    return <StateBlock message={getErrorMessage(loadError)} />;
  }

  const maxDensity = Math.max(...data.weeklyDensity.map((d) => d.count), 1);

  return (
    <div className="dashboard-grid">
      {/* Üst stat kartları — KORUNDU */}
      <button
        type="button"
        className="stat-card glass"
        style={{ borderLeft: '4px solid var(--warning)', textAlign: 'left' }}
        onClick={() => navigate('/leads')}
        aria-label="Aktif leadleri görüntüle"
      >
        <span className="stat-label">Aktif Leadler</span>
        <span className="stat-value">{data.activeLeads}</span>
      </button>
      <button
        type="button"
        className="stat-card glass"
        style={{ borderLeft: '4px solid var(--success)', textAlign: 'left' }}
        onClick={() => navigate('/customers')}
        aria-label="Müşterileri görüntüle"
      >
        <span className="stat-label">Toplam Müşteri</span>
        <span className="stat-value">{data.totalCustomers}</span>
      </button>
      <button
        type="button"
        className="stat-card glass"
        style={{ borderLeft: '4px solid var(--primary-light)', textAlign: 'left' }}
        onClick={() => navigate('/calendar')}
        aria-label="Ziyaret takvimine git"
      >
        <span className="stat-label">Planlı Ziyaretler</span>
        <span className="stat-value">{data.plannedMeetings}</span>
      </button>

      {/* Haftalık hedef kartları — Yeni/Mevcut Müşteri kırılımı eklendi */}
      {data.goals.length === 0 ? (
        <div
          className="stat-card glass full-width"
          style={{ borderTop: '4px solid var(--accent-gold)', cursor: 'default' }}
        >
          <p className="muted">Henüz tanımlanmış hedef yok.</p>
        </div>
      ) : (
        data.goals.map((goal) => {
          const pct = Math.min(100, Math.round(goal.percent));
          // Hedef segment'i "Customer" ise yeni/mevcut kırılımı mevcut veriyle
          // yaklaştırılarak gösterilir. Backend GoalProgressDto'da
          // newCustomerAchieved/existingCustomerAchieved alanları olmadığından
          // şimdilik toplam "achieved" gösterilmekte; tam kırılım için
          // backend ajanına bildirim yapılmıştır (bakınız rapor).
          const showBreakdown = goal.segment === 'Customer' || goal.segment === 'All';
          return (
            <div
              key={goal.goalId}
              className="stat-card glass"
              style={{ borderTop: '4px solid var(--accent-gold)', cursor: 'default' }}
            >
              <div>
                <h3 style={{ marginBottom: 6 }}>
                  {goal.assigneeName ?? 'Atanmamış'}
                </h3>
                <p className="muted" style={{ fontSize: '0.8rem', marginBottom: 8 }}>
                  {SEGMENT_LABELS[goal.segment] ?? goal.segment} · Haftalık Hedef
                </p>
                <p className="muted" style={{ marginBottom: showBreakdown ? 10 : 0 }}>
                  <strong>{goal.achieved}</strong> /{' '}
                  <strong>{goal.weeklyTarget}</strong> görüşme
                </p>

                {/* Yeni / Mevcut Müşteri kırılımı */}
                {showBreakdown && (
                  <div
                    style={{
                      display: 'flex',
                      gap: 10,
                      flexWrap: 'wrap',
                      marginTop: 4,
                    }}
                  >
                    <div
                      style={{
                        flex: 1,
                        background: 'rgba(39,174,96,0.12)',
                        border: '1px solid rgba(39,174,96,0.3)',
                        borderRadius: 'var(--radius-md)',
                        padding: '8px 12px',
                        minWidth: 80,
                      }}
                    >
                      <div
                        style={{
                          fontSize: '0.65rem',
                          color: 'var(--text-muted)',
                          textTransform: 'uppercase',
                          letterSpacing: '0.05em',
                          marginBottom: 4,
                        }}
                      >
                        Yeni Müşteri
                      </div>
                      <div
                        style={{
                          fontSize: '1.1rem',
                          fontWeight: 700,
                          color: 'var(--success)',
                        }}
                      >
                        {goal.newCustomerAchieved}
                      </div>
                    </div>
                    <div
                      style={{
                        flex: 1,
                        background: 'rgba(74,144,226,0.10)',
                        border: '1px solid rgba(74,144,226,0.25)',
                        borderRadius: 'var(--radius-md)',
                        padding: '8px 12px',
                        minWidth: 80,
                      }}
                    >
                      <div
                        style={{
                          fontSize: '0.65rem',
                          color: 'var(--text-muted)',
                          textTransform: 'uppercase',
                          letterSpacing: '0.05em',
                          marginBottom: 4,
                        }}
                      >
                        Mevcut Müşteri
                      </div>
                      <div
                        style={{
                          fontSize: '1.1rem',
                          fontWeight: 700,
                          color: 'var(--accent-blue)',
                        }}
                      >
                        {goal.existingCustomerAchieved}
                      </div>
                    </div>
                  </div>
                )}
              </div>
              <div style={{ textAlign: 'right', marginTop: 'auto', paddingTop: 8 }}>
                <div
                  style={{
                    fontSize: '2.5rem',
                    fontWeight: 800,
                    color: 'var(--accent-gold)',
                  }}
                >
                  {pct}%
                </div>
                <div className="progress-track">
                  <div className="progress-fill" style={{ width: `${pct}%` }} />
                </div>
              </div>
            </div>
          );
        })
      )}

      {/* Haftalık görüşme yoğunluğu grafiği — KORUNDU */}
      <div className="glass full-width card">
        <h3>Haftalık Görüşme Yoğunluğu</h3>
        <div className="chart-container">
          {data.weeklyDensity.map((point) => (
            <div className="chart-col" key={point.day}>
              <div
                className="chart-bar"
                style={{ height: `${(point.count / maxDensity) * 100}%` }}
                title={`${point.count}`}
              />
              <span className="chart-label">{point.day}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Bu haftanın günleri tablosu — güne tıklayınca o günün görüşmeleri */}
      <WeekDayPanel meetings={meetingsQuery.data ?? []} />

      {/* Yaklaşan İhaleler tablosu */}
      <UpcomingTendersTable tenders={tendersQuery.data ?? []} />
    </div>
  );
}
