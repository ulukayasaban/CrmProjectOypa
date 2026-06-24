import { useState } from 'react';
import { reportApi } from '../features/reports/api/reportApi';
import { saveBlob } from '../shared/lib/saveBlob';
import { getErrorMessage } from '../shared/lib/errorMessage';

export default function ReportsPage() {
  const [meetingDownloading, setMeetingDownloading] = useState(false);
  const [meetingError, setMeetingError] = useState<string | null>(null);

  const [tenderDownloading, setTenderDownloading] = useState(false);
  const [tenderError, setTenderError] = useState<string | null>(null);

  async function handleMeetingDownload() {
    setMeetingDownloading(true);
    setMeetingError(null);
    try {
      const blob = await reportApi.downloadMeetingReport();
      saveBlob(blob, 'Gorusme-Raporu.xlsx');
    } catch (err) {
      setMeetingError(getErrorMessage(err));
    } finally {
      setMeetingDownloading(false);
    }
  }

  async function handleTenderDownload() {
    setTenderDownloading(true);
    setTenderError(null);
    try {
      // Görüşme raporu indirme yardımcısını yeniden kullan (saveBlob)
      const blob = await reportApi.downloadTenderReport();
      saveBlob(blob, 'Ihale-Raporu.xlsx');
    } catch (err) {
      setTenderError(getErrorMessage(err));
    } finally {
      setTenderDownloading(false);
    }
  }

  return (
    <>
      <div className="page-head">
        <h3>Raporlar</h3>
      </div>
      <div className="report-cards">
        {/* Görüşme Raporu */}
        <div className="glass card" style={{ maxWidth: 380 }}>
          <h4 style={{ marginBottom: 8 }}>Görüşme Raporu (Excel)</h4>
          <p className="muted" style={{ fontSize: '0.85rem', marginBottom: 16 }}>
            Tüm görüşme kayıtlarını, temsilci bilgilerini ve görüşme notlarını
            içeren Excel dosyasını indir.
          </p>
          {meetingError && (
            <div className="form-error" style={{ marginBottom: 8, fontSize: '0.8rem' }}>
              {meetingError}
            </div>
          )}
          <button
            type="button"
            className="btn btn-primary"
            disabled={meetingDownloading}
            onClick={() => void handleMeetingDownload()}
          >
            {meetingDownloading ? 'İndiriliyor...' : 'İndir'}
          </button>
        </div>

        {/* İhale Raporu */}
        <div className="glass card" style={{ maxWidth: 380 }}>
          <h4 style={{ marginBottom: 8 }}>İhale Raporu (Excel)</h4>
          <p className="muted" style={{ fontSize: '0.85rem', marginBottom: 16 }}>
            Tüm ihale kayıtlarını, firma bilgilerini ve ihale durumlarını
            içeren Excel dosyasını indir.
          </p>
          {tenderError && (
            <div className="form-error" style={{ marginBottom: 8, fontSize: '0.8rem' }}>
              {tenderError}
            </div>
          )}
          <button
            type="button"
            className="btn btn-primary"
            disabled={tenderDownloading}
            onClick={() => void handleTenderDownload()}
          >
            {tenderDownloading ? 'İndiriliyor...' : 'İndir'}
          </button>
        </div>
      </div>
    </>
  );
}
