import { useState } from 'react';
import { Navigate, useNavigate, useParams } from 'react-router-dom';
import { useTendersPaged } from '../features/tenders/model/useTenders';
import { useChangeTenderStatus, useDeleteTender } from '../features/tenders/model/useTenders';
import { TenderFormModal } from '../features/tenders/ui/TenderFormModal';
import { Modal } from '../shared/components/Modal';
import { Pagination } from '../shared/components/Pagination';
import { SortableTh } from '../shared/components/SortableTh';
import { TableSkeleton } from '../shared/components/TableSkeleton';
import { StateBlock } from '../shared/components/StateBlock';
import { PlusIcon } from '../shared/components/icons';
import { useToast } from '../shared/components/toast/ToastProvider';
import { useConfirm } from '../shared/hooks/useConfirm';
import { useDebouncedValue } from '../shared/hooks/useDebouncedValue';
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

/**
 * Her segmentin hangi TenderStatus değerlerini kapsadığını tanımlar.
 * Sunucu tek bir status parametresi aldığından, segment filtrelemesi
 * client-side olarak sayfalı sonuçlara uygulanır.
 */
const SEGMENT_STATUSES: Record<Segment, TenderStatus[]> = {
  aktif: ['Hazirlik', 'TeklifVerildi'],
  kazanilan: ['Kazanildi'],
  kaybedilen: ['Kaybedildi', 'Iptal'],
};

const VALID_SEGMENTS: Segment[] = ['aktif', 'kazanilan', 'kaybedilen'];

function isSegment(value: string): value is Segment {
  return VALID_SEGMENTS.includes(value as Segment);
}

/** İhale tablosu sıralanabilir sütunları. */
type TenderSortField = 'title' | 'company' | 'tenderDate' | 'estimatedValue' | 'status';

const DEFAULT_SORT_BY: TenderSortField = 'tenderDate';
const DEFAULT_SORT_DIR = 'desc' as const;
const DEFAULT_PAGE_SIZE = 20;

export default function TendersPage() {
  const { segment = '' } = useParams<{ segment: string }>();

  if (!isSegment(segment)) {
    return <Navigate to="/tenders/aktif" replace />;
  }

  // key={segment}: segment değişince sayfa/arama/sıralama state'i sıfırlanır.
  return <TendersContent key={segment} segment={segment} />;
}

interface TendersContentProps {
  segment: Segment;
}

function TendersContent({ segment }: TendersContentProps) {
  const navigate = useNavigate();
  const toast = useToast();
  const { confirm, ConfirmEl } = useConfirm();

  const [sectorFilter, setSectorFilter] = useState<Sector | ''>('');
  const [searchInput, setSearchInput] = useState('');
  const [sortBy, setSortBy] = useState<TenderSortField>(DEFAULT_SORT_BY);
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>(DEFAULT_SORT_DIR);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [createModal, setCreateModal] = useState(false);
  const [editTender, setEditTender] = useState<TenderDto | null>(null);
  const [statusChangeTender, setStatusChangeTender] = useState<TenderDto | null>(null);

  // 300ms gecikme ile arama
  const search = useDebouncedValue(searchInput, 300);

  const { data, isLoading, isError, error } = useTendersPaged({
    search: search || undefined,
    sortBy,
    sortDir,
    page,
    pageSize,
    sector: sectorFilter !== '' ? sectorFilter : undefined,
    // Segment'in tüm statüleri sunucuya gönderilir → filtre sunucu tarafında;
    // böylece sayfalama ve toplam kayıt sayısı segment için doğru olur.
    statuses: SEGMENT_STATUSES[segment],
  });

  const changeTenderStatus = useChangeTenderStatus();
  const deleteTender = useDeleteTender();

  /** Sıralama değişince sayfayı başa al. */
  function handleSort(field: string, dir: 'asc' | 'desc') {
    setSortBy(field as TenderSortField);
    setSortDir(dir);
    setPage(1);
  }

  /** Arama değişince sayfayı başa al. */
  function handleSearchChange(value: string) {
    setSearchInput(value);
    setPage(1);
  }

  /** Sektör filtresi değişince sayfayı başa al. */
  function handleSectorChange(value: Sector | '') {
    setSectorFilter(value);
    setPage(1);
  }

  /** Sayfa boyutu değişince sayfayı başa al. */
  function handlePageSizeChange(size: number) {
    setPageSize(size);
    setPage(1);
  }

  async function handleDelete(tender: TenderDto) {
    const confirmed = await confirm({
      title: 'İhaleyi Sil',
      message: `"${tender.title}" ihalesini silmek istediğinizden emin misiniz?`,
      confirmLabel: 'Sil',
      danger: true,
    });
    if (!confirmed) return;

    try {
      await deleteTender.mutateAsync(tender.id);
      toast.success('İhale silindi.');
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  }

  // Segment filtresi sunucuda uygulandığından sayfa içeriği doğrudan kullanılır.
  const filtered = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;
  const totalCount = data?.totalCount ?? 0;

  if (isLoading) return <TableSkeleton columns={11} />;
  if (isError) return <StateBlock message={getErrorMessage(error)} />;

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
        <div style={{ display: 'flex', gap: 10, alignItems: 'center', flexWrap: 'wrap' }}>
          {/* Arama kutusu */}
          <input
            type="search"
            placeholder="İhale veya firma ara..."
            aria-label="İhale ara"
            value={searchInput}
            onChange={(event) => handleSearchChange(event.target.value)}
            style={{ minWidth: 200 }}
          />
          <select
            value={sectorFilter}
            aria-label="İş koluna göre filtrele"
            onChange={(event) =>
              handleSectorChange(toSectorFilter(event.target.value))
            }
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
              <SortableTh
                field="title"
                activeSortBy={sortBy}
                activeSortDir={sortDir}
                onSort={handleSort}
              >
                Başlık
              </SortableTh>
              <SortableTh
                field="company"
                activeSortBy={sortBy}
                activeSortDir={sortDir}
                onSort={handleSort}
              >
                Firma
              </SortableTh>
              <th>İş Kolu</th>
              <SortableTh
                field="tenderDate"
                activeSortBy={sortBy}
                activeSortDir={sortDir}
                onSort={handleSort}
              >
                İhale Tarihi
              </SortableTh>
              <SortableTh
                field="estimatedValue"
                activeSortBy={sortBy}
                activeSortDir={sortDir}
                onSort={handleSort}
              >
                Tahmini Değer
              </SortableTh>
              <th>Personel</th>
              <th>Hacim</th>
              <th>Miktar</th>
              <SortableTh
                field="status"
                activeSortBy={sortBy}
                activeSortDir={sortDir}
                onSort={handleSort}
              >
                Durum
              </SortableTh>
              <th>İşlemler</th>
            </tr>
          </thead>
          <tbody>
            {filtered.length === 0 && (
              <tr>
                <td colSpan={11} className="table-empty">
                  {search
                    ? `"${search}" için bu kategoride ihale bulunamadı.`
                    : 'Bu kategoride ihale bulunamadı.'}
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
                      onClick={() => void handleDelete(tender)}
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

      <Pagination
        page={page}
        totalPages={totalPages}
        totalCount={totalCount}
        pageSize={pageSize}
        onPageChange={setPage}
        onPageSizeChange={handlePageSizeChange}
      />

      {ConfirmEl}

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
              {
                onSuccess: () => {
                  setStatusChangeTender(null);
                  toast.success('İhale durumu güncellendi.');
                },
                onError: (err) => {
                  toast.error(getErrorMessage(err));
                },
              },
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
