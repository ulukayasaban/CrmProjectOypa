import { useState } from 'react';
import { useNavigate, useParams, Navigate } from 'react-router-dom';
import { useCustomersPaged } from '../features/companies/model/useCompanies';
import { CategoryBadges } from '../features/categories/ui/CategoryBadges';
import { useCategories } from '../features/categories/model/useCategories';
import { Pagination } from '../shared/components/Pagination';
import { SortableTh } from '../shared/components/SortableTh';
import { TableSkeleton } from '../shared/components/TableSkeleton';
import { StateBlock } from '../shared/components/StateBlock';
import { useDebouncedValue } from '../shared/hooks/useDebouncedValue';
import { SECTOR_LABELS, SERVICE_SECTOR_LABELS } from '../shared/constants/labels';
import { formatDate } from '../shared/lib/format';
import { getErrorMessage } from '../shared/lib/errorMessage';
import type { CustomerStatus } from '../shared/types/enums';

/** Segment'ten CustomerStatus eşlemesi. `tumu` → undefined (status gönderilmez). */
const SEGMENT_MAP: Record<string, CustomerStatus | undefined> = {
  aktif: 'Active',
  pasif: 'Passive',
  tumu: undefined,
};

const SEGMENT_TITLE: Record<string, string> = {
  aktif: 'Aktif Müşteri Portföyü',
  pasif: 'Pasif Müşteri Portföyü',
  tumu: 'Tüm Müşteriler',
};

/** Geçerli segment anahtarları. */
const VALID_SEGMENTS = new Set(['aktif', 'pasif', 'tumu']);

/** Müşteri tablosu sıralanabilir sütunları. */
type CustomerSortField = 'title' | 'city' | 'createdAt';

const DEFAULT_SORT_BY: CustomerSortField = 'title';
const DEFAULT_SORT_DIR = 'asc' as const;
const DEFAULT_PAGE_SIZE = 20;

export default function CustomersPage() {
  const navigate = useNavigate();
  const { segment = '' } = useParams<{ segment: string }>();

  // Geçersiz segment → aktif'e yönlendir
  if (!VALID_SEGMENTS.has(segment)) {
    return <Navigate to="/customers/aktif" replace />;
  }

  // SEGMENT_MAP[segment] undefined olabilir (tumu), bu kasıtlı.
  const status = SEGMENT_MAP[segment];

  return (
    <CustomersContent
      status={status}
      title={SEGMENT_TITLE[segment]}
      navigate={navigate}
    />
  );
}

interface CustomersContentProps {
  status: CustomerStatus | undefined;
  title: string;
  navigate: ReturnType<typeof useNavigate>;
}

function CustomersContent({ status, title, navigate }: CustomersContentProps) {
  const [searchInput, setSearchInput] = useState('');
  const [sortBy, setSortBy] = useState<CustomerSortField>(DEFAULT_SORT_BY);
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>(DEFAULT_SORT_DIR);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [categoryId, setCategoryId] = useState<string | undefined>(undefined);

  // 300ms gecikme ile arama
  const search = useDebouncedValue(searchInput, 300);

  const categories = useCategories();

  const { data, isLoading, isError, error } = useCustomersPaged({
    status,
    search: search || undefined,
    sortBy,
    sortDir,
    page,
    pageSize,
    categoryId,
  });

  /** Sıralama değişince sayfayı başa al. */
  function handleSort(field: string, dir: 'asc' | 'desc') {
    setSortBy(field as CustomerSortField);
    setSortDir(dir);
    setPage(1);
  }

  /** Arama değişince sayfayı başa al. */
  function handleSearchChange(value: string) {
    setSearchInput(value);
    setPage(1);
  }

  /** Sayfa boyutu değişince sayfayı başa al. */
  function handlePageSizeChange(size: number) {
    setPageSize(size);
    setPage(1);
  }

  /** Kategori filtresi değişince sayfayı başa al. */
  function handleCategoryChange(id: string) {
    setCategoryId(id === '' ? undefined : id);
    setPage(1);
  }

  const items = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;
  const totalCount = data?.totalCount ?? 0;

  if (isLoading) return <TableSkeleton columns={9} />;
  if (isError || !data) return <StateBlock message={getErrorMessage(error)} />;

  return (
    <>
      <div className="page-head">
        <div>
          <h3>{title}</h3>
          <p className="muted" style={{ fontSize: '0.9rem' }}>
            Müşteri portföyünüzü görüntüleyin ve firma dosyalarına erişin.
          </p>
        </div>
      </div>

      {/* Arama kutusu ve kategori filtresi */}
      <div style={{ marginBottom: 12, display: 'flex', gap: 10, flexWrap: 'wrap' }}>
        <input
          type="search"
          placeholder="Firma adı veya şehir ara..."
          aria-label="Müşteri ara"
          value={searchInput}
          onChange={(event) => handleSearchChange(event.target.value)}
          style={{ maxWidth: 320 }}
        />
        <select
          aria-label="Kategori filtrele"
          value={categoryId ?? ''}
          onChange={(event) => handleCategoryChange(event.target.value)}
          style={{ minWidth: 160 }}
        >
          <option value="">Tüm Kategoriler</option>
          {(categories.data ?? []).map((cat) => (
            <option key={cat.id} value={cat.id}>
              {cat.name}
            </option>
          ))}
        </select>
      </div>

      <div className="data-table-container glass">
        <table className="data-table">
          <thead>
            <tr>
              <SortableTh
                field="title"
                activeSortBy={sortBy}
                activeSortDir={sortDir}
                onSort={handleSort}
              >
                Firma
              </SortableTh>
              <th>Sektör</th>
              <SortableTh
                field="createdAt"
                activeSortBy={sortBy}
                activeSortDir={sortDir}
                onSort={handleSort}
              >
                Aktif Geçiş Tarihi
              </SortableTh>
              <th>Kategoriler</th>
              <th>Hizmet Sektörü</th>
              <th>Atanan Lead</th>
              <th>İletişim</th>
              <th>Temsilci</th>
              <th>İşlem</th>
            </tr>
          </thead>
          <tbody>
            {items.length === 0 && (
              <tr>
                <td colSpan={9} className="table-empty">
                  {search
                    ? `"${search}" için sonuç bulunamadı.`
                    : status === 'Active'
                      ? 'Henüz aktif müşteriniz bulunmuyor.'
                      : status === 'Passive'
                        ? 'Henüz pasif müşteriniz bulunmuyor.'
                        : 'Henüz kayıtlı müşteriniz bulunmuyor.'}
                </td>
              </tr>
            )}
            {items.map((company) => (
              <tr
                key={company.id}
                className="clickable"
                onClick={() => navigate(`/companies/${company.id}`)}
              >
                <td>
                  <strong>{company.title}</strong>
                  {company.isNewCustomer && (
                    <span
                      className="badge badge-lead"
                      style={{ marginLeft: 8, fontSize: '0.65rem', verticalAlign: 'middle' }}
                      title="Yeni müşteri"
                    >
                      YENİ
                    </span>
                  )}
                </td>
                <td>
                  <span className="badge badge-opportunity">
                    {SECTOR_LABELS[company.sector]}
                  </span>
                </td>
                <td style={{ fontSize: '0.85rem' }}>
                  {formatDate(company.activatedAtUtc)}
                </td>
                <td>
                  <CategoryBadges categories={company.categories} />
                </td>
                <td style={{ fontSize: '0.85rem' }}>
                  {company.serviceSector ? (
                    <span className="badge badge-neutral">
                      {SERVICE_SECTOR_LABELS[company.serviceSector]}
                    </span>
                  ) : (
                    <span className="muted">-</span>
                  )}
                </td>
                <td style={{ fontSize: '0.85rem' }}>
                  {company.leadOwnerName ?? <span className="muted">-</span>}
                </td>
                <td style={{ fontSize: '0.85rem' }}>{company.email}</td>
                <td style={{ fontSize: '0.85rem' }}>
                  {company.assignedSalesRepName ?? 'Havuz'}
                </td>
                <td>
                  <button
                    type="button"
                    className="btn btn-ghost btn-sm"
                    onClick={(event) => {
                      event.stopPropagation();
                      navigate(`/companies/${company.id}`);
                    }}
                  >
                    Dosyayı Aç
                  </button>
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
    </>
  );
}
