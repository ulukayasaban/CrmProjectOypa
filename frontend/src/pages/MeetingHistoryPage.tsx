import React, { useState } from 'react';
import { useMeetingsPaged } from '../features/meetings/model/useMeetings';
import { MeetingNotes } from '../features/meetings/ui/MeetingNotes';
import { reportApi } from '../features/reports/api/reportApi';
import { saveBlob } from '../shared/lib/saveBlob';
import { Pagination } from '../shared/components/Pagination';
import { SortableTh } from '../shared/components/SortableTh';
import { TableSkeleton } from '../shared/components/TableSkeleton';
import { StateBlock } from '../shared/components/StateBlock';
import { useDebouncedValue } from '../shared/hooks/useDebouncedValue';
import {
  MEETING_METHOD_LABELS,
  MEETING_STATUS_LABELS,
} from '../shared/constants/labels';
import { formatDate, formatTime } from '../shared/lib/format';
import { getErrorMessage } from '../shared/lib/errorMessage';
import type { MeetingDto } from '../entities/meeting/model/meeting';

/** Görüşme tablosu sıralanabilir sütunları. */
type MeetingSortField = 'date' | 'company' | 'status';

const DEFAULT_SORT_BY: MeetingSortField = 'date';
const DEFAULT_SORT_DIR = 'desc' as const;
const DEFAULT_PAGE_SIZE = 20;

export default function MeetingHistoryPage() {
  const [searchInput, setSearchInput] = useState('');
  const [sortBy, setSortBy] = useState<MeetingSortField>(DEFAULT_SORT_BY);
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>(DEFAULT_SORT_DIR);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [downloading, setDownloading] = useState(false);

  // 300ms gecikme ile arama
  const search = useDebouncedValue(searchInput, 300);

  const { data, isLoading, isError, error } = useMeetingsPaged({
    search: search || undefined,
    sortBy,
    sortDir,
    page,
    pageSize,
  });

  async function handleExport() {
    setDownloading(true);
    try {
      const blob = await reportApi.downloadMeetingReport();
      saveBlob(blob, 'Gorusme-Raporu.xlsx');
    } finally {
      setDownloading(false);
    }
  }

  /** Sıralama değişince sayfayı başa al ve açık satırı kapat. */
  function handleSort(field: string, dir: 'asc' | 'desc') {
    setSortBy(field as MeetingSortField);
    setSortDir(dir);
    setPage(1);
    setExpandedId(null);
  }

  /** Arama değişince sayfayı başa al. */
  function handleSearchChange(value: string) {
    setSearchInput(value);
    setPage(1);
    setExpandedId(null);
  }

  /** Sayfa boyutu değişince sayfayı başa al. */
  function handlePageSizeChange(size: number) {
    setPageSize(size);
    setPage(1);
    setExpandedId(null);
  }

  const items = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;
  const totalCount = data?.totalCount ?? 0;

  if (isLoading) return <TableSkeleton columns={7} />;
  if (isError || !data) return <StateBlock message={getErrorMessage(error)} />;

  return (
    <>
      <div className="page-head">
        <h3>Görüşme Geçmişi</h3>
        <button
          type="button"
          className="btn btn-primary btn-sm"
          disabled={downloading}
          onClick={() => void handleExport()}
        >
          {downloading ? 'İndiriliyor...' : "Excel'e Aktar"}
        </button>
      </div>

      {/* Arama kutusu */}
      <div style={{ marginBottom: 12 }}>
        <input
          type="search"
          placeholder="Firma adı veya temsilci ara..."
          aria-label="Görüşme ara"
          value={searchInput}
          onChange={(event) => handleSearchChange(event.target.value)}
          style={{ maxWidth: 320 }}
        />
      </div>

      <div className="data-table-container glass">
        <table className="data-table">
          <thead>
            <tr>
              <SortableTh
                field="company"
                activeSortBy={sortBy}
                activeSortDir={sortDir}
                onSort={handleSort}
              >
                Firma
              </SortableTh>
              <th>İlgili Kişi</th>
              <th>Temsilci</th>
              <SortableTh
                field="date"
                activeSortBy={sortBy}
                activeSortDir={sortDir}
                onSort={handleSort}
              >
                Tarih / Saat
              </SortableTh>
              <th>Yöntem</th>
              <SortableTh
                field="status"
                activeSortBy={sortBy}
                activeSortDir={sortDir}
                onSort={handleSort}
              >
                Durum
              </SortableTh>
              <th>Notlar</th>
            </tr>
          </thead>
          <tbody>
            {items.length === 0 && (
              <tr>
                <td colSpan={7} className="table-empty">
                  {search
                    ? `"${search}" için sonuç bulunamadı.`
                    : 'Görüşme kaydı yok.'}
                </td>
              </tr>
            )}
            {items.map((meeting: MeetingDto) => {
              const isExpanded = expandedId === meeting.id;
              const noteCount = meeting.notes.length;

              return (
                <React.Fragment key={meeting.id}>
                  <tr>
                    <td>
                      <strong>{meeting.companyTitle}</strong>
                    </td>
                    <td style={{ fontSize: '0.85rem' }}>
                      {meeting.contactName ?? '-'}
                    </td>
                    <td style={{ fontSize: '0.85rem' }}>
                      <div>{meeting.salesRepName}</div>
                      {meeting.salesRepTitle && (
                        <div className="muted" style={{ fontSize: '0.75rem' }}>
                          {meeting.salesRepTitle}
                        </div>
                      )}
                    </td>
                    <td style={{ fontSize: '0.85rem' }}>
                      {formatDate(meeting.date)}
                      <br />
                      {formatTime(meeting.time)}
                    </td>
                    <td style={{ fontSize: '0.85rem' }}>
                      {MEETING_METHOD_LABELS[meeting.method]}
                    </td>
                    <td>
                      <span
                        className={`badge ${
                          meeting.status === 'Done'
                            ? 'badge-customer'
                            : meeting.status === 'Cancelled'
                              ? 'badge-danger'
                              : 'badge-lead'
                        }`}
                      >
                        {MEETING_STATUS_LABELS[meeting.status]}
                      </span>
                    </td>
                    <td>
                      <button
                        type="button"
                        className="btn btn-ghost btn-sm"
                        onClick={() =>
                          setExpandedId(isExpanded ? null : meeting.id)
                        }
                      >
                        {noteCount > 0
                          ? `${noteCount} not ${isExpanded ? '▴' : '▾'}`
                          : `Not Ekle ${isExpanded ? '▴' : '▾'}`}
                      </button>
                    </td>
                  </tr>
                  {isExpanded && (
                    <tr>
                      <td
                        colSpan={7}
                        style={{
                          padding: '8px 16px',
                          background: 'rgba(0,0,0,0.15)',
                        }}
                      >
                        <MeetingNotes
                          meetingId={meeting.id}
                          companyId={meeting.companyId}
                          notes={meeting.notes}
                        />
                      </td>
                    </tr>
                  )}
                </React.Fragment>
              );
            })}
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
