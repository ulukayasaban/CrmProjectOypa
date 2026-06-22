import React, { useState } from 'react';
import { useMeetings } from '../features/meetings/model/useMeetings';
import { MeetingNotes } from '../features/meetings/ui/MeetingNotes';
import { reportApi } from '../features/reports/api/reportApi';
import { saveBlob } from '../shared/lib/saveBlob';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import {
  MEETING_METHOD_LABELS,
  MEETING_STATUS_LABELS,
} from '../shared/constants/labels';
import { formatDate, formatTime } from '../shared/lib/format';
import { getErrorMessage } from '../shared/lib/errorMessage';
import type { MeetingDto } from '../entities/meeting/model/meeting';

export default function MeetingHistoryPage() {
  const { data, isLoading, isError, error } = useMeetings();
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [downloading, setDownloading] = useState(false);

  async function handleExport() {
    setDownloading(true);
    try {
      const blob = await reportApi.downloadMeetingReport();
      saveBlob(blob, 'Gorusme-Raporu.xlsx');
    } finally {
      setDownloading(false);
    }
  }

  if (isLoading) return <Spinner />;
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
      <div className="data-table-container glass">
        <table className="data-table">
          <thead>
            <tr>
              <th>Firma</th>
              <th>İlgili Kişi</th>
              <th>Temsilci</th>
              <th>Tarih / Saat</th>
              <th>Yöntem</th>
              <th>Durum</th>
              <th>Notlar</th>
            </tr>
          </thead>
          <tbody>
            {data.length === 0 && (
              <tr>
                <td colSpan={7} className="table-empty">
                  Görüşme kaydı yok.
                </td>
              </tr>
            )}
            {data.map((meeting: MeetingDto) => {
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
                        style={{ padding: '8px 16px', background: 'rgba(0,0,0,0.15)' }}
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
    </>
  );
}
