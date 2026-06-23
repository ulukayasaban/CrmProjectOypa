import { useNavigate } from 'react-router-dom';
import { useDashboard } from '../features/dashboard/model/useDashboard';
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

  if (isLoading) return <Spinner />;
  if (isError || !data) {
    return <StateBlock message={getErrorMessage(error)} />;
  }

  const maxDensity = Math.max(...data.weeklyDensity.map((d) => d.count), 1);

  return (
    <div className="dashboard-grid">
      {/* Tıklanabilir stat kartları: erişilebilirlik için <button> olarak işaretlendi.
          .stat-card görsel stili korunur; tarayıcı varsayılan button stilini sıfırlamak
          için inline style eklenir (CSS sınıfı yeterli değilse). */}
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
                <p className="muted" style={{ fontSize: '0.8rem', marginBottom: 4 }}>
                  {SEGMENT_LABELS[goal.segment] ?? goal.segment}
                </p>
                <p className="muted">
                  <strong>{goal.achieved}</strong> /{' '}
                  <strong>{goal.weeklyTarget}</strong> görüşme
                </p>
              </div>
              <div style={{ textAlign: 'right' }}>
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
    </div>
  );
}
