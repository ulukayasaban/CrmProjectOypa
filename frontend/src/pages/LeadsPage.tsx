import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useLeadsPaged } from '../features/companies/model/useCompanies';
import { CompanyFormModal } from '../features/companies/ui/CompanyFormModal';
import { StatusBadge } from '../features/companies/ui/StatusBadge';
import { Pagination } from '../shared/components/Pagination';
import { SortableTh } from '../shared/components/SortableTh';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { PlusIcon } from '../shared/components/icons';
import { useDebouncedValue } from '../shared/hooks/useDebouncedValue';
import { LEAD_STATUS_LABELS, SECTOR_LABELS } from '../shared/constants/labels';
import { getErrorMessage } from '../shared/lib/errorMessage';
import type { LeadStatus } from '../shared/types/enums';

type LeadStatusFilter = LeadStatus | undefined;

const LEAD_TABS: ReadonlyArray<{ value: LeadStatusFilter; label: string }> = [
  { value: undefined, label: 'Tümü' },
  { value: 'New', label: LEAD_STATUS_LABELS.New },
  { value: 'Contacted', label: LEAD_STATUS_LABELS.Contacted },
  { value: 'Qualified', label: LEAD_STATUS_LABELS.Qualified },
  { value: 'Lost', label: LEAD_STATUS_LABELS.Lost },
];

/** Lead tablosu sıralanabilir sütunları. */
type LeadSortField = 'title' | 'city' | 'createdAt';

const DEFAULT_SORT_BY: LeadSortField = 'createdAt';
const DEFAULT_SORT_DIR = 'desc' as const;
const DEFAULT_PAGE_SIZE = 20;

export default function LeadsPage() {
  const navigate = useNavigate();
  const [modalOpen, setModalOpen] = useState(false);
  const [activeStatus, setActiveStatus] = useState<LeadStatusFilter>(undefined);
  const [searchInput, setSearchInput] = useState('');
  const [sortBy, setSortBy] = useState<LeadSortField>(DEFAULT_SORT_BY);
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>(DEFAULT_SORT_DIR);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);

  // 300ms gecikme ile arama — sunucuya aşırı istek gitmesini önler
  const search = useDebouncedValue(searchInput, 300);

  const { data, isLoading, isError, error } = useLeadsPaged({
    status: activeStatus,
    search: search || undefined,
    sortBy,
    sortDir,
    page,
    pageSize,
  });

  /** Sıralama değişince sayfayı başa al. */
  function handleSort(field: string, dir: 'asc' | 'desc') {
    setSortBy(field as LeadSortField);
    setSortDir(dir);
    setPage(1);
  }

  /** Durum sekmesi değişince sayfa + aramayı sıfırla. */
  function handleTabChange(status: LeadStatusFilter) {
    setActiveStatus(status);
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

  const items = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;
  const totalCount = data?.totalCount ?? 0;

  return (
    <>
      <div className="page-head">
        <h3>Lead & Fırsat Listesi</h3>
        <button
          type="button"
          className="btn btn-primary"
          onClick={() => setModalOpen(true)}
        >
          <PlusIcon /> Yeni Firma / Fırsat
        </button>
      </div>

      {/* Durum sekmeleri */}
      <div className="tab-bar">
        {LEAD_TABS.map((tab) => (
          <button
            key={tab.value ?? 'all'}
            type="button"
            className={`tab-btn${activeStatus === tab.value ? ' tab-btn--active' : ''}`}
            onClick={() => handleTabChange(tab.value)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Arama kutusu */}
      <div style={{ marginBottom: 12 }}>
        <input
          type="search"
          placeholder="Firma adı veya şehir ara..."
          aria-label="Lead ara"
          value={searchInput}
          onChange={(event) => handleSearchChange(event.target.value)}
          style={{ maxWidth: 320 }}
        />
      </div>

      {isLoading && <Spinner />}
      {isError && <StateBlock message={getErrorMessage(error)} />}

      {!isLoading && !isError && (
        <>
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
                    Firma Ünvanı
                  </SortableTh>
                  <th>Sektör</th>
                  <SortableTh
                    field="city"
                    activeSortBy={sortBy}
                    activeSortDir={sortDir}
                    onSort={handleSort}
                  >
                    Adres
                  </SortableTh>
                  <th>Durum</th>
                  <th>Temsilci</th>
                  <th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {items.length === 0 && (
                  <tr>
                    <td colSpan={6} className="table-empty">
                      {search
                        ? `"${search}" için sonuç bulunamadı.`
                        : 'Sistemde lead bulunmuyor.'}
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
                    </td>
                    <td>
                      <span className="badge badge-neutral">
                        {SECTOR_LABELS[company.sector]}
                      </span>
                    </td>
                    <td className="muted" style={{ fontSize: '0.85rem' }}>
                      {company.address}
                    </td>
                    <td>
                      <StatusBadge company={company} />
                    </td>
                    <td style={{ fontSize: '0.85rem' }}>
                      {company.assignedSalesRepName ?? (
                        <span className="muted">Havuz</span>
                      )}
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
                        Detay / İşlem
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
      )}

      {modalOpen && (
        <CompanyFormModal
          onClose={() => setModalOpen(false)}
          onCreated={(company) => navigate(`/companies/${company.id}`)}
        />
      )}
    </>
  );
}
