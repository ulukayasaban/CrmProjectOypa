import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMeetings } from '../features/meetings/model/useMeetings';
import { useTenders } from '../features/tenders/model/useTenders';
import { MeetingFormModal } from '../features/meetings/ui/MeetingFormModal';
import { TenderStatusBadge } from '../features/tenders/ui/TenderStatusBadge';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { PlusIcon } from '../shared/components/icons';
import { MEETING_METHOD_LABELS } from '../shared/constants/labels';
import { formatTime } from '../shared/lib/format';
import { getErrorMessage } from '../shared/lib/errorMessage';
import type { MeetingDto } from '../entities/meeting/model/meeting';

const WEEKDAYS = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];
const MONTH_NAMES = [
  'Ocak',
  'Şubat',
  'Mart',
  'Nisan',
  'Mayıs',
  'Haziran',
  'Temmuz',
  'Ağustos',
  'Eylül',
  'Ekim',
  'Kasım',
  'Aralık',
];

function toDateKey(year: number, month: number, day: number): string {
  const mm = String(month + 1).padStart(2, '0');
  const dd = String(day).padStart(2, '0');
  return `${year}-${mm}-${dd}`;
}

export default function CalendarPage() {
  const navigate = useNavigate();
  const meetings = useMeetings();
  const tenders = useTenders();
  const today = new Date();
  const [viewYear, setViewYear] = useState(today.getFullYear());
  const [viewMonth, setViewMonth] = useState(today.getMonth());
  const [selectedDate, setSelectedDate] = useState<string>(
    toDateKey(today.getFullYear(), today.getMonth(), today.getDate()),
  );
  const [meetingModal, setMeetingModal] = useState(false);
  // Düzenleme modunda açmak için seçili görüşme; undefined → yeni randevu
  const [editingMeeting, setEditingMeeting] = useState<MeetingDto | undefined>(undefined);

  const isLoading = meetings.isLoading || tenders.isLoading;
  const isError = meetings.isError || tenders.isError;
  const loadError = meetings.error ?? tenders.error;

  // Her güne ait görüşme (ziyaret) ve ihale sayılarını ayrı tutar; böylece
  // takvim hücresinde tip bazlı (altın = görüşme, mavi = ihale) nokta gösterilir.
  const eventsByDate = useMemo(() => {
    const map = new Map<string, { meetings: number; tenders: number }>();
    const ensure = (key: string) => {
      let entry = map.get(key);
      if (!entry) {
        entry = { meetings: 0, tenders: 0 };
        map.set(key, entry);
      }
      return entry;
    };
    for (const meeting of meetings.data ?? []) {
      ensure(meeting.date.slice(0, 10)).meetings += 1;
    }
    for (const tender of tenders.data ?? []) {
      ensure(tender.tenderDate.slice(0, 10)).tenders += 1;
    }
    return map;
  }, [meetings.data, tenders.data]);

  const dayMeetings = useMemo(
    () => (meetings.data ?? []).filter((m) => m.date.slice(0, 10) === selectedDate),
    [meetings.data, selectedDate],
  );

  const dayTenders = useMemo(
    () => (tenders.data ?? []).filter((t) => t.tenderDate.slice(0, 10) === selectedDate),
    [tenders.data, selectedDate],
  );

  if (isLoading) return <Spinner />;
  if (isError) return <StateBlock message={getErrorMessage(loadError)} />;

  const firstDay = new Date(viewYear, viewMonth, 1);
  // Convert Sunday-based getDay() to a Monday-based offset.
  const startOffset = (firstDay.getDay() + 6) % 7;
  const daysInMonth = new Date(viewYear, viewMonth + 1, 0).getDate();
  const todayKey = toDateKey(
    today.getFullYear(),
    today.getMonth(),
    today.getDate(),
  );

  function goPrevMonth() {
    if (viewMonth === 0) {
      setViewMonth(11);
      setViewYear((y) => y - 1);
    } else {
      setViewMonth((m) => m - 1);
    }
  }

  function goNextMonth() {
    if (viewMonth === 11) {
      setViewMonth(0);
      setViewYear((y) => y + 1);
    } else {
      setViewMonth((m) => m + 1);
    }
  }

  return (
    <>
      <div className="page-head">
        <div>
          <h3>Ziyaret Takvimi</h3>
          <p className="muted" style={{ fontSize: '0.9rem' }}>
            Planlarınızı yönetmek için günlere tıklayın.
          </p>
        </div>
        <button
          type="button"
          className="btn btn-primary"
          onClick={() => {
            setEditingMeeting(undefined);
            setMeetingModal(true);
          }}
        >
          <PlusIcon /> Yeni Randevu
        </button>
      </div>

      <div className="calendar-layout">
        <div className="calendar-grid glass">
          <div className="cal-header">
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              onClick={goPrevMonth}
            >
              ‹
            </button>
            <span>
              {MONTH_NAMES[viewMonth]} {viewYear}
            </span>
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              onClick={goNextMonth}
            >
              ›
            </button>
          </div>
          <div className="cal-legend">
            <span className="cal-legend-item">
              <span className="cal-dot cal-dot--meeting" /> Görüşme
            </span>
            <span className="cal-legend-item">
              <span className="cal-dot cal-dot--tender" /> İhale
            </span>
          </div>
          <div className="cal-weekdays">
            {WEEKDAYS.map((day) => (
              <div className="cal-weekday" key={day}>
                {day}
              </div>
            ))}
          </div>
          <div className="cal-days">
            {Array.from({ length: startOffset }).map((_, index) => (
              <div className="cal-cell empty" key={`empty-${index}`} />
            ))}
            {Array.from({ length: daysInMonth }).map((_, index) => {
              const day = index + 1;
              const key = toDateKey(viewYear, viewMonth, day);
              const flags = eventsByDate.get(key);
              const classes = ['cal-cell'];
              if (key === todayKey) classes.push('today');
              if (key === selectedDate) classes.push('selected');
              return (
                <div
                  key={key}
                  className={classes.join(' ')}
                  onClick={() => setSelectedDate(key)}
                >
                  <span className="cal-day-num">{day}</span>
                  {flags && (
                    <span className="cal-dots">
                      {flags.meetings > 0 && (
                        <span className="cal-dot cal-dot--meeting" />
                      )}
                      {flags.tenders > 0 && (
                        <span className="cal-dot cal-dot--tender" />
                      )}
                    </span>
                  )}
                </div>
              );
            })}
          </div>
        </div>

        <div className="glass card">
          <h4>Seçili Gün Etkinlikleri</h4>
          <p className="muted" style={{ fontSize: '0.8rem', marginTop: 4 }}>
            {selectedDate}
          </p>
          <div
            style={{
              marginTop: 20,
              display: 'flex',
              flexDirection: 'column',
              gap: 10,
            }}
          >
            {dayMeetings.length === 0 && dayTenders.length === 0 && (
              <p className="muted" style={{ fontSize: '0.85rem' }}>
                Bu tarihte planlı görüşme veya ihale yok.
              </p>
            )}
            {dayMeetings.map((meeting) => (
              <div
                key={meeting.id}
                className="glass cal-event cal-event--meeting"
                role="button"
                tabIndex={0}
                title="Düzenlemek için tıklayın"
                onClick={() => {
                  // Seçili görüşmeyle düzenleme modalını aç
                  setEditingMeeting(meeting);
                  setMeetingModal(true);
                }}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    setEditingMeeting(meeting);
                    setMeetingModal(true);
                  }
                }}
              >
                <div className="cal-event-head">
                  <span className="badge badge-meeting">Görüşme</span>
                  <span className="muted" style={{ fontSize: '0.75rem' }}>
                    {formatTime(meeting.time)}
                  </span>
                </div>
                <div className="cal-event-title">{meeting.companyTitle}</div>
                <div className="muted" style={{ fontSize: '0.75rem' }}>
                  {MEETING_METHOD_LABELS[meeting.method]}
                </div>
              </div>
            ))}
            {dayTenders.map((tender) => (
              <div
                key={tender.id}
                className="glass cal-event cal-event--tender"
                title="Detayı görmek için tıklayın"
                onClick={() => navigate(`/tenders/detay/${tender.id}`)}
                role="button"
                tabIndex={0}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    navigate(`/tenders/detay/${tender.id}`);
                  }
                }}
              >
                <div className="cal-event-head">
                  <span className="badge badge-tender">İhale</span>
                  <TenderStatusBadge status={tender.status} />
                </div>
                <div className="cal-event-title">{tender.title}</div>
                <div className="muted" style={{ fontSize: '0.75rem' }}>
                  {tender.companyTitle}
                </div>
              </div>
            ))}
            <button
              type="button"
              className="btn btn-ghost btn-sm btn-block"
              onClick={() => {
                setEditingMeeting(undefined);
                setMeetingModal(true);
              }}
            >
              + Randevu Ekle
            </button>
          </div>
        </div>
      </div>

      {meetingModal && (
        <MeetingFormModal
          defaultDate={editingMeeting ? undefined : selectedDate}
          meeting={editingMeeting}
          onClose={() => {
            setMeetingModal(false);
            setEditingMeeting(undefined);
          }}
        />
      )}
    </>
  );
}
