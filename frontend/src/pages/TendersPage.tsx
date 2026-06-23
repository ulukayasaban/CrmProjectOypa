import { useState } from 'react';
import { Navigate, useNavigate, useParams } from 'react-router-dom';
import { useTenders } from '../features/tenders/model/useTenders';
import { useChangeTenderStatus, useDeleteTender } from '../features/tenders/model/useTenders';
import { TenderFormModal } from '../features/tenders/ui/TenderFormModal';
import { Modal } from '../shared/components/Modal';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { PlusIcon } from '../shared/components/icons';
import {
  SECTOR_LABELS,
  SECTOR_OPTIONS,
  TENDER_STATUS_LABELS,
  TENDER_STATUS_OPTIONS,
} from '../shared/constants/labels';
import { formatDate } from '../shared/lib/format';
import { getErrorMessage } from '../shared/lib/errorMessage';
import { toSectorFilter, isTenderStatus } from '../shared/lib/narrowing';
import type { TenderDto, TenderStatus } from '../entities/tender/model/tender';
import type { Sector } from '../shared/types/enums';

type Segment = 'aktif' | 'kazanilan' | 'kaybedilen';

const SEGMENT_STATUSES: Record<Segment, TenderStatus[]> = {
  aktif: ['Hazirlik', 'TeklifVerildi'],
  kazanilan: ['Kazanildi'],
  kaybedilen: ['Kaybedildi', 'Iptal'],
};

const VALID_SEGMENTS: Segment[] = ['aktif', 'kazanilan', 'kaybedilen'];

function isSegment(value: string): value is Segment {
  return VALID_SEGMENTS.includes(value as Segment);
}

export default function TendersPage() {
  const { segment = '' } = useParams<{ segment: string }>();

  if (!isSegment(segment)) {
    return <Navigate to="/tenders/aktif" replace />;
  }

  return <TendersContent segment={segment} />;
}

interface TendersContentProps {
  segment: Segment;
}

function TendersContent({ segment }: TendersContentProps) {
  const navigate = useNavigate();
  const [sectorFilter, setSectorFilter] = useState<Sector | ''>('');
  const [createModal, setCreateModal] = useState(false);
  const [editTender, setEditTender] = useState<TenderDto | null>(null);
  const [statusChangeTender, setStatusChangeTender] = useState<TenderDto | null>(null);

  const { data, isLoading, isError, error } = useTenders();
  const changeTenderStatus = useChangeTenderStatus();
  const deleteTender = useDeleteTender();

  if (isLoading) return <Spinner />;
  if (isError) return <StateBlock message={getErrorMessage(error)} />;

  const allowedStatuses = SEGMENT_STATUSES[segment];

  const filtered = (data ?? []).filter((tender) => {
    const statusMatch = allowedStatuses.includes(tender.status);
    const sectorMatch = sectorFilter === '' || tender.sector === sectorFilter;
    return statusMatch && sectorMatch;
  });

  function handleDelete(tender: TenderDto) {
    if (
      window.confirm(
        `"${tender.title}" ihalesini silmek istediğinizden emin misiniz?`,
      )
    ) {
      deleteTender.mutate(tender.id);
    }
  }

  return (
    <>
      <div className="page-head">
        <div>
          <h3>
            {segment === 'aktif'
              ? 'Aktif İhaleler'
              : segment === 'kazanilan'
                ? 'Kazanılan İhaleler'
                : 'Kaybedilen İhaleler'}
          </h3>
        </div>
        <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
          <select
            value={sectorFilter}
            aria-label="İş koluna göre filtrele"
            onChange={(event) => setSectorFilter(toSectorFilter(event.target.value))}
            style={{ minWidth: 160 }}
          >
            <option value="">Tüm İş Kolları</option>
            {SECTOR_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => setCreateModal(true)}
          >
            <PlusIcon /> Yeni İhale
          </button>
        </div>
      </div>

      <div className="data-table-container glass">
        <table className="data-table">
          <thead>
            <tr>
              <th>İhale No</th>
              <th>Başlık</th>
              <th>Firma</th>
              <th>İş Kolu</th>
              <th>İhale Tarihi</th>
              <th>Tahmini Değer</th>
              <th>Personel</th>
              <th>Hacim</th>
              <th>Miktar</th>
              <th>Durum</th>
              <th>İşlemler</th>
            </tr>
          </thead>
          <tbody>
            {filtered.length === 0 && (
              <tr>
                <td colSpan={11} className="table-empty">
                  Bu kategoride ihale bulunamadı.
                </td>
              </tr>
            )}
            {filtered.map((tender) => (
              <tr key={tender.id}>
                <td style={{ fontSize: '0.8rem' }}>
                  {tender.tenderNumber ?? '-'}
                </td>
                <td className="cell-wrap">
                  <strong>{tender.title}</strong>
                </td>
                <td style={{ fontSize: '0.85rem' }}>{tender.companyTitle}</td>
                <td>
                  <span className="badge badge-opportunity">
                    {SECTOR_LABELS[tender.sector]}
                  </span>
                </td>
                <td style={{ fontSize: '0.85rem' }}>
                  {formatDate(tender.tenderDate)}
                </td>
                <td style={{ fontSize: '0.85rem' }}>
                  {tender.estimatedValue != null
                    ? tender.estimatedValue.toLocaleString('tr-TR', {
                        style: 'currency',
                        currency: 'TRY',
                        minimumFractionDigits: 0,
                        maximumFractionDigits: 0,
                      })
                    : '-'}
                </td>
                <td style={{ fontSize: '0.85rem' }}>
                  {tender.personnelCount ?? '-'}
                </td>
                <td style={{ fontSize: '0.85rem' }}>
                  {tender.volume ?? '-'}
                </td>
                <td style={{ fontSize: '0.85rem' }}>
                  {tender.quantity ?? '-'}
                </td>
                <td>
                  <TenderStatusBadge status={tender.status} />
                </td>
                <td>
                  <div className="row-actions">
                    <button
                      type="button"
                      className="btn btn-ghost btn-sm"
                      onClick={() => navigate(`/tenders/detay/${tender.id}`)}
                    >
                      Detay
                    </button>
                    <button
                      type="button"
                      className="btn btn-ghost btn-sm"
                      onClick={() => setEditTender(tender)}
                    >
                      Düzenle
                    </button>
                    <button
                      type="button"
                      className="btn btn-ghost btn-sm"
                      onClick={() => setStatusChangeTender(tender)}
                    >
                      Durum
                    </button>
                    <button
                      type="button"
                      className="btn btn-ghost btn-sm"
                      disabled={deleteTender.isPending}
                      onClick={() => handleDelete(tender)}
                    >
                      Sil
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {createModal && <TenderFormModal onClose={() => setCreateModal(false)} />}
      {editTender && (
        <TenderFormModal
          tender={editTender}
          onClose={() => setEditTender(null)}
        />
      )}
      {statusChangeTender && (
        <TenderStatusModal
          tender={statusChangeTender}
          onClose={() => setStatusChangeTender(null)}
          onSave={(status) => {
            changeTenderStatus.mutate(
              { id: statusChangeTender.id, status },
              { onSuccess: () => setStatusChangeTender(null) },
            );
          }}
          isPending={changeTenderStatus.isPending}
        />
      )}
    </>
  );
}

interface TenderStatusBadgeProps {
  status: TenderStatus;
}

function TenderStatusBadge({ status }: TenderStatusBadgeProps) {
  const className =
    status === 'Kazanildi'
      ? 'badge badge-customer'
      : status === 'Kaybedildi' || status === 'Iptal'
        ? 'badge badge-danger'
        : status === 'TeklifVerildi'
          ? 'badge badge-lead'
          : 'badge badge-opportunity';

  return <span className={className}>{TENDER_STATUS_LABELS[status]}</span>;
}

interface TenderStatusModalProps {
  tender: TenderDto;
  onClose: () => void;
  onSave: (status: TenderStatus) => void;
  isPending: boolean;
}

function TenderStatusModal({
  tender,
  onClose,
  onSave,
  isPending,
}: TenderStatusModalProps) {
  const [selected, setSelected] = useState<TenderStatus>(tender.status);

  return (
    <Modal title="Durum Değiştir" onClose={onClose} width={400}>
      <p className="muted" style={{ fontSize: '0.85rem', marginBottom: 16 }}>
        {tender.title}
      </p>
      <div className="form-group">
        <label htmlFor="tenderStatus">Yeni Durum</label>
        <select
          id="tenderStatus"
          value={selected}
          onChange={(event) => {
            // TENDER_STATUS_OPTIONS yalnızca geçerli TenderStatus değerlerini içerir;
            // narrowing helper ile runtime güvenliği sağlanır.
            if (isTenderStatus(event.target.value)) {
              setSelected(event.target.value);
            }
          }}
        >
          {TENDER_STATUS_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </div>
      <div className="modal-footer">
        <button type="button" className="btn btn-ghost" onClick={onClose}>
          İptal
        </button>
        <button
          type="button"
          className="btn btn-primary"
          disabled={isPending}
          onClick={() => onSave(selected)}
        >
          {isPending ? 'Kaydediliyor...' : 'Kaydet'}
        </button>
      </div>
    </Modal>
  );
}
