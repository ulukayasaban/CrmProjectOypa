import { useState } from 'react';
import { reportApi } from '../features/reports/api/reportApi';
import { saveBlob } from '../shared/lib/saveBlob';
import { getErrorMessage } from '../shared/lib/errorMessage';

export default function ReportsPage() {
  const [downloading, setDownloading] = useState(false);
  const [downloadError, setDownloadError] = useState<string | null>(null);

  async function handleDownload() {
    setDownloading(true);
    setDownloadError(null);
    try {
      const blob = await reportApi.downloadMeetingReport();
      saveBlob(blob, 'Gorusme-Raporu.xlsx');
    } catch (err) {
      setDownloadError(getErrorMessage(err));
    } finally {
      setDownloading(false);
    }
  }

  return (
    <>
      <div className="page-head">
        <h3>Raporlar</h3>
      </div>
      <div className="report-cards">
        <div className="glass card" style={{ maxWidth: 380 }}>
          <h4 style={{ marginBottom: 8 }}>Görüşme Raporu (Excel)</h4>
          <p className="muted" style={{ fontSize: '0.85rem', marginBottom: 16 }}>
            Tüm görüşme kayıtlarını, temsilci bilgilerini ve görüşme notlarını
            içeren Excel dosyasını indir.
          </p>
          {downloadError && (
            <div className="form-error" style={{ marginBottom: 8, fontSize: '0.8rem' }}>
              {downloadError}
            </div>
          )}
          <button
            type="button"
            className="btn btn-primary"
            disabled={downloading}
            onClick={() => void handleDownload()}
          >
            {downloading ? 'İndiriliyor...' : 'İndir'}
          </button>
        </div>
      </div>
    </>
  );
}
