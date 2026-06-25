import { useState } from 'react';
import {
  reportApi,
  type ReportDateRange,
} from '../features/reports/api/reportApi';
import { saveBlob } from '../shared/lib/saveBlob';
import { getErrorMessage } from '../shared/lib/errorMessage';

/** Bir rapor kartının tanımı. */
interface ReportCard {
  key: string;
  title: string;
  description: string;
  fileName: string;
  /** true ise üstteki tarih aralığı filtresi bu rapora uygulanır. */
  dated: boolean;
  download: (range?: ReportDateRange) => Promise<Blob>;
}

const REPORTS: ReportCard[] = [
  {
    key: 'meetings',
    title: 'Görüşme Raporu',
    description:
      'Görüşme kayıtları, temsilci bilgileri ve görüşme notları. Tarih aralığı görüşme tarihine uygulanır.',
    fileName: 'Gorusme-Raporu.xlsx',
    dated: true,
    download: (range) => reportApi.downloadMeetingReport(range),
  },
  {
    key: 'tenders',
    title: 'İhale Raporu',
    description:
      'İhale kayıtları, firma bilgileri ve ihale durumları. Tarih aralığı ihale tarihine uygulanır.',
    fileName: 'Ihale-Raporu.xlsx',
    dated: true,
    download: (range) => reportApi.downloadTenderReport(range),
  },
  {
    key: 'goals',
    title: 'Hedef Raporu',
    description:
      'Aktif hedefler; atanan personel, haftalık hedef ve toplam gerçekleşen ilerleme.',
    fileName: 'Hedef-Raporu.xlsx',
    dated: false,
    download: () => reportApi.downloadGoalReport(),
  },
  {
    key: 'customers',
    title: 'Müşteri Raporu',
    description:
      'Müşteri (pipeline) listesi; iş kolu, durum, iletişim ve atanan temsilci. Tarih aralığı oluşturulma tarihine uygulanır.',
    fileName: 'Musteri-Raporu.xlsx',
    dated: true,
    download: (range) => reportApi.downloadCustomerReport(range),
  },
];

export default function ReportsPage() {
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [error, setError] = useState<{ key: string; message: string } | null>(
    null,
  );

  async function handleDownload(card: ReportCard) {
    setBusyKey(card.key);
    setError(null);
    try {
      const range: ReportDateRange | undefined = card.dated
        ? { from: from || undefined, to: to || undefined }
        : undefined;
      const blob = await card.download(range);
      saveBlob(blob, card.fileName);
    } catch (err) {
      setError({ key: card.key, message: getErrorMessage(err) });
    } finally {
      setBusyKey(null);
    }
  }

  return (
    <>
      <div className="page-head">
        <h3>Raporlar</h3>
      </div>

      {/* Tarih aralığı filtresi — tarih bazlı raporlara (Görüşme, İhale, Müşteri) uygulanır. */}
      <div
        className="glass card"
        style={{ marginBottom: 20, display: 'flex', flexWrap: 'wrap', gap: 16, alignItems: 'flex-end' }}
      >
        <div className="form-group" style={{ margin: 0 }}>
          <label htmlFor="report-from">Başlangıç</label>
          <input
            id="report-from"
            type="date"
            value={from}
            onChange={(e) => setFrom(e.target.value)}
            onClick={(e) => {
              try {
                e.currentTarget.showPicker();
              } catch {
                /* showPicker desteklenmiyor */
              }
            }}
          />
        </div>
        <div className="form-group" style={{ margin: 0 }}>
          <label htmlFor="report-to">Bitiş</label>
          <input
            id="report-to"
            type="date"
            value={to}
            onChange={(e) => setTo(e.target.value)}
            onClick={(e) => {
              try {
                e.currentTarget.showPicker();
              } catch {
                /* showPicker desteklenmiyor */
              }
            }}
          />
        </div>
        {(from || to) && (
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={() => {
              setFrom('');
              setTo('');
            }}
          >
            Aralığı Temizle
          </button>
        )}
        <p className="muted" style={{ fontSize: '0.75rem', margin: 0, flexBasis: '100%' }}>
          Boş bırakılırsa tüm kayıtlar dahil edilir. Hedef raporu tarih aralığından etkilenmez.
        </p>
      </div>

      <div className="report-cards">
        {REPORTS.map((card) => (
          <div key={card.key} className="glass card" style={{ maxWidth: 380 }}>
            <h4 style={{ marginBottom: 8 }}>
              {card.title} (Excel)
              {card.dated && (from || to) && (
                <span
                  className="badge badge-lead"
                  style={{ marginLeft: 8, fontSize: '0.65rem' }}
                >
                  Filtreli
                </span>
              )}
            </h4>
            <p className="muted" style={{ fontSize: '0.85rem', marginBottom: 16 }}>
              {card.description}
            </p>
            {error?.key === card.key && (
              <div className="form-error" style={{ marginBottom: 8, fontSize: '0.8rem' }}>
                {error.message}
              </div>
            )}
            <button
              type="button"
              className="btn btn-primary"
              disabled={busyKey === card.key}
              onClick={() => void handleDownload(card)}
            >
              {busyKey === card.key ? 'İndiriliyor...' : 'İndir'}
            </button>
          </div>
        ))}
      </div>
    </>
  );
}
