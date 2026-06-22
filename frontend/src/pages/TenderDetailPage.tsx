import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTender, useChangeTenderStatus } from '../features/tenders/model/useTenders';
import { useCompany } from '../features/companies/model/useCompanies';
import { TenderFormModal } from '../features/tenders/ui/TenderFormModal';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import {
  SECTOR_LABELS,
  TENDER_STATUS_LABELS,
  TENDER_STATUS_OPTIONS,
} from '../shared/constants/labels';
import { formatDate } from '../shared/lib/format';
import { getErrorMessage } from '../shared/lib/errorMessage';
import type { TenderStatus } from '../entities/tender/model/tender';

export default function TenderDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [editModal, setEditModal] = useState(false);
  const [statusSelected, setStatusSelected] = useState<TenderStatus | null>(null);

  const tender = useTender(id);
  const changeTenderStatus = useChangeTenderStatus();

  // Load company master data using companyId from the tender
  const companyId = tender.data?.companyId ?? '';
  const company = useCompany(companyId);

  if (tender.isLoading) return <Spinner />;
  if (tender.isError || !tender.data) {
    return <StateBlock message={getErrorMessage(tender.error)} />;
  }

  const t = tender.data;
  const c = company.data;

  const statusClass =
    t.status === 'Kazanildi'
      ? 'badge badge-customer'
      : t.status === 'Kaybedildi' || t.status === 'Iptal'
        ? 'badge badge-danger'
        : t.status === 'TeklifVerildi'
          ? 'badge badge-lead'
          : 'badge badge-opportunity';

  function handleStatusChange() {
    if (!statusSelected) return;
    changeTenderStatus.mutate({ id: t.id, status: statusSelected });
  }

  return (
    <>
      <div className="page-head">
        <button
          type="button"
          className="btn btn-ghost btn-sm"
          onClick={() => navigate(-1)}
        >
          &larr; Geri
        </button>
        <button
          type="button"
          className="btn btn-ghost"
          onClick={() => setEditModal(true)}
        >
          Düzenle
        </button>
      </div>

      <div className="detail-grid">
        {/* Left column: tender details */}
        <div className="detail-col">
          <div className="glass card">
            <h3 style={{ marginBottom: 6 }}>{t.title}</h3>
            {t.tenderNumber && (
              <p className="muted" style={{ fontSize: '0.85rem', marginBottom: 10 }}>
                İhale No: {t.tenderNumber}
              </p>
            )}
            <div style={{ marginBottom: 12 }}>
              <span className={statusClass}>{TENDER_STATUS_LABELS[t.status]}</span>
            </div>
            <hr className="divider" />
            <div className="detail-info">
              <span>
                <strong>İş Kolu:</strong>
                <br />
                {SECTOR_LABELS[t.sector]}
              </span>
              <span>
                <strong>İhale Tarihi:</strong>
                <br />
                {formatDate(t.tenderDate)}
              </span>
              <span>
                <strong>Firma:</strong>
                <br />
                {t.companyTitle}
              </span>
              <span>
                <strong>Sorumlu Temsilci:</strong>
                <br />
                {t.assignedSalesRepName ?? '-'}
              </span>
              {t.personnelCount != null && (
                <span>
                  <strong>Personel Sayısı:</strong>
                  <br />
                  {t.personnelCount}
                </span>
              )}
              {t.estimatedValue != null && (
                <span>
                  <strong>Tahmini Değer:</strong>
                  <br />
                  {t.estimatedValue.toLocaleString('tr-TR', {
                    style: 'currency',
                    currency: 'TRY',
                    minimumFractionDigits: 0,
                    maximumFractionDigits: 0,
                  })}
                </span>
              )}
              {t.volume != null && (
                <span>
                  <strong>Hacim:</strong>
                  <br />
                  {t.volume}
                </span>
              )}
              {t.quantity != null && (
                <span>
                  <strong>Miktar:</strong>
                  <br />
                  {t.quantity}
                </span>
              )}
              {t.description && (
                <span style={{ gridColumn: '1 / -1' }}>
                  <strong>Açıklama:</strong>
                  <br />
                  {t.description}
                </span>
              )}
              <span className="muted" style={{ fontSize: '0.75rem' }}>
                Oluşturulma: {formatDate(t.createdAtUtc)}
              </span>
            </div>

            <hr className="divider" />
            <div className="form-group" style={{ marginTop: 12 }}>
              <label htmlFor="statusChange">Durum Değiştir</label>
              <div style={{ display: 'flex', gap: 8 }}>
                <select
                  id="statusChange"
                  value={statusSelected ?? t.status}
                  onChange={(event) =>
                    setStatusSelected(event.target.value as TenderStatus)
                  }
                >
                  {TENDER_STATUS_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                <button
                  type="button"
                  className="btn btn-primary"
                  disabled={
                    changeTenderStatus.isPending ||
                    (statusSelected === null || statusSelected === t.status)
                  }
                  onClick={handleStatusChange}
                >
                  {changeTenderStatus.isPending ? 'Kaydediliyor...' : 'Güncelle'}
                </button>
              </div>
              {changeTenderStatus.isError && (
                <span className="field-error">
                  {getErrorMessage(changeTenderStatus.error)}
                </span>
              )}
            </div>
          </div>
        </div>

        {/* Right column: company master data */}
        <div className="detail-col">
          <div className="glass card">
            <h4 style={{ marginBottom: 12 }}>Müşteri Master Datası</h4>
            {company.isLoading && (
              <p className="muted" style={{ fontSize: '0.85rem' }}>
                Firma bilgisi yükleniyor...
              </p>
            )}
            {company.isError && (
              <p className="muted" style={{ fontSize: '0.85rem' }}>
                Firma bilgisi alınamadı.
              </p>
            )}
            {c && (
              <div className="detail-info">
                <span>
                  <strong>Firma Adı:</strong>
                  <br />
                  {c.title}
                </span>
                <span>
                  <strong>İş Kolu:</strong>
                  <br />
                  {SECTOR_LABELS[c.sector]}
                </span>
                {c.city && (
                  <span>
                    <strong>Şehir:</strong>
                    <br />
                    {c.city}
                  </span>
                )}
                <span>
                  <strong>Telefon:</strong>
                  <br />
                  {c.phone}
                </span>
                <span>
                  <strong>E-posta:</strong>
                  <br />
                  {c.email}
                </span>
                {c.taxNumber && (
                  <span>
                    <strong>Vergi No:</strong>
                    <br />
                    {c.taxNumber}
                  </span>
                )}
                <span>
                  <strong>Atanan Temsilci:</strong>
                  <br />
                  {c.assignedSalesRepName ?? 'Havuz (OYPA)'}
                </span>
                {c.address && (
                  <span style={{ gridColumn: '1 / -1' }}>
                    <strong>Adres:</strong>
                    <br />
                    {c.address}
                  </span>
                )}
              </div>
            )}
            <div style={{ marginTop: 16 }}>
              <button
                type="button"
                className="btn btn-ghost btn-sm"
                onClick={() => navigate(`/companies/${t.companyId}`)}
              >
                Firma Dosyasını Aç
              </button>
            </div>
          </div>
        </div>
      </div>

      {editModal && (
        <TenderFormModal
          tender={t}
          onClose={() => setEditModal(false)}
        />
      )}
    </>
  );
}
