import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import type { TenderDto } from '../../../entities/tender/model/tender';
import { TenderStatusBadge } from '../../tenders/ui/TenderStatusBadge';
import { formatDate } from '../../../shared/lib/format';

interface UpcomingTendersTableProps {
  tenders: TenderDto[];
  /** Gösterilecek maksimum satır sayısı (varsayılan 8). */
  maxRows?: number;
}

/**
 * Dashboard — Yaklaşan İhaleler tablosu.
 * tenderDate >= bugün olan ihaleleri tarihe göre artan sırada gösterir.
 */
export function UpcomingTendersTable({
  tenders,
  maxRows = 8,
}: UpcomingTendersTableProps) {
  const navigate = useNavigate();

  const todayStr = useMemo(() => {
    const t = new Date();
    return `${t.getFullYear()}-${String(t.getMonth() + 1).padStart(2, '0')}-${String(t.getDate()).padStart(2, '0')}`;
  }, []);

  const upcoming = useMemo(() => {
    return tenders
      .filter((t) => t.tenderDate.slice(0, 10) >= todayStr)
      .sort((a, b) => a.tenderDate.localeCompare(b.tenderDate))
      .slice(0, maxRows);
  }, [tenders, todayStr, maxRows]);

  return (
    <div className="glass full-width card">
      <div className="card-head">
        <h3>Yaklaşan İhaleler</h3>
        <span className="muted" style={{ fontSize: '0.8rem' }}>
          {upcoming.length === 0 ? 'Yaklaşan ihale yok' : `${upcoming.length} ihale`}
        </span>
      </div>

      {upcoming.length === 0 ? (
        <p className="muted" style={{ fontSize: '0.85rem' }}>
          Önümüzdeki günlerde planlanmış ihale bulunmuyor.
        </p>
      ) : (
        <div className="data-table-container" style={{ marginTop: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Firma</th>
                <th>Başlık</th>
                <th>İhale Tarihi</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {upcoming.map((tender) => (
                <tr
                  key={tender.id}
                  className="clickable"
                  onClick={() => navigate(`/tenders/detay/${tender.id}`)}
                  style={{ cursor: 'pointer' }}
                >
                  <td>
                    <strong>{tender.companyTitle}</strong>
                  </td>
                  <td className="cell-wrap">{tender.title}</td>
                  <td style={{ whiteSpace: 'nowrap' }}>
                    {formatDate(tender.tenderDate)}
                  </td>
                  <td>
                    <TenderStatusBadge status={tender.status} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
