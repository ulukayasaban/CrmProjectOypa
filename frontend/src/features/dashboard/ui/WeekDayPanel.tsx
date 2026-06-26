import { useMemo, useState } from 'react';
import type { MeetingDto } from '../../../entities/meeting/model/meeting';
import { MEETING_STATUS_LABELS } from '../../../shared/constants/labels';
import { formatTime } from '../../../shared/lib/format';

interface WeekDayPanelProps {
  meetings: MeetingDto[];
}

/**
 * Dashboard — Haftanın Günleri tablosu.
 * Bu haftanın Pzt–Paz aralığını hesaplar; her güne ait görüşme sayısını gösterir.
 * Bir güne tıklayınca o günün görüşmeleri alt panel olarak açılır.
 */
export function WeekDayPanel({ meetings }: WeekDayPanelProps) {
  const [selectedKey, setSelectedKey] = useState<string | null>(null);

  // Bu haftanın Pzt–Paz tarih listesini hesapla
  const weekDays = useMemo(() => {
    const today = new Date();
    // JS'te 0=Pazar; Pazartesi'ye normalize et
    const dayOfWeek = today.getDay(); // 0(Sun)..6(Sat)
    const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;
    const monday = new Date(today);
    monday.setDate(today.getDate() + mondayOffset);

    const TR_DAYS = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];
    return TR_DAYS.map((label, i) => {
      const d = new Date(monday);
      d.setDate(monday.getDate() + i);
      const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
      return { label, key, date: d };
    });
  }, []);

  const todayKey = useMemo(() => {
    const t = new Date();
    return `${t.getFullYear()}-${String(t.getMonth() + 1).padStart(2, '0')}-${String(t.getDate()).padStart(2, '0')}`;
  }, []);

  // Gün bazlı görüşme haritası
  const meetingsByDay = useMemo(() => {
    const map = new Map<string, MeetingDto[]>();
    for (const m of meetings) {
      const key = m.date.slice(0, 10);
      const existing = map.get(key);
      if (existing) {
        existing.push(m);
      } else {
        map.set(key, [m]);
      }
    }
    return map;
  }, [meetings]);

  const selectedMeetings = selectedKey ? (meetingsByDay.get(selectedKey) ?? []) : [];

  function handleDayClick(key: string) {
    setSelectedKey((prev) => (prev === key ? null : key));
  }

  return (
    <div className="glass full-width card">
      <h3 style={{ marginBottom: 16 }}>Bu Haftanın Görüşme Dağılımı</h3>

      {/* Gün satırı */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(7, 1fr)',
          gap: 8,
        }}
      >
        {weekDays.map(({ label, key }) => {
          const count = meetingsByDay.get(key)?.length ?? 0;
          const isToday = key === todayKey;
          const isSelected = key === selectedKey;
          return (
            <button
              key={key}
              type="button"
              onClick={() => handleDayClick(key)}
              style={{
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                gap: 6,
                padding: '12px 4px',
                borderRadius: 'var(--radius-lg)',
                border: isSelected
                  ? '2px solid var(--accent-gold)'
                  : isToday
                    ? '2px solid rgba(227,6,19,0.4)'
                    : '1px solid var(--glass-border)',
                background: isSelected
                  ? 'rgba(227,6,19,0.15)'
                  : isToday
                    ? 'rgba(227,6,19,0.07)'
                    : 'var(--surface-1)',
                cursor: 'pointer',
                transition: 'var(--transition-smooth)',
                color: 'var(--text-main)',
                fontFamily: 'inherit',
              }}
              aria-pressed={isSelected}
              aria-label={`${label} — ${count} görüşme`}
            >
              <span
                style={{
                  fontSize: '0.7rem',
                  color: isToday ? 'var(--accent-gold)' : 'var(--text-muted)',
                  fontWeight: isToday ? 700 : 400,
                  textTransform: 'uppercase',
                }}
              >
                {label}
              </span>
              <span
                style={{
                  fontSize: '1.4rem',
                  fontWeight: 700,
                  color: count > 0 ? 'var(--text-on-dark)' : 'var(--text-muted)',
                }}
              >
                {count}
              </span>
              {count > 0 && (
                <span
                  style={{
                    width: 6,
                    height: 6,
                    borderRadius: '50%',
                    background: 'var(--accent-gold)',
                    flexShrink: 0,
                  }}
                />
              )}
            </button>
          );
        })}
      </div>

      {/* Seçili günün görüşme listesi */}
      {selectedKey !== null && (
        <div
          style={{
            marginTop: 20,
            borderTop: '1px solid var(--glass-border)',
            paddingTop: 16,
          }}
        >
          {selectedMeetings.length === 0 ? (
            <p className="muted" style={{ fontSize: '0.85rem' }}>
              Bu gün için planlanmış görüşme yok.
            </p>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
              {selectedMeetings.map((m) => {
                const isDone = m.status === 'Done';
                const isCancelled = m.status === 'Cancelled';
                return (
                  <div
                    key={m.id}
                    className="glass"
                    style={{
                      padding: '10px 14px',
                      borderRadius: 'var(--radius-md)',
                      borderLeft: `3px solid ${isDone ? 'var(--success)' : isCancelled ? 'var(--error)' : 'var(--accent-gold)'}`,
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      gap: 12,
                      flexWrap: 'wrap',
                    }}
                  >
                    <div>
                      <div style={{ fontWeight: 700, fontSize: '0.88rem' }}>
                        {m.companyTitle}
                      </div>
                      <div className="muted" style={{ fontSize: '0.75rem', marginTop: 2 }}>
                        {formatTime(m.time)}
                        {m.contactName ? ` · ${m.contactName}` : ''}
                      </div>
                    </div>
                    <span
                      className={`badge ${isDone ? 'badge-customer' : isCancelled ? 'badge-danger' : 'badge-lead'}`}
                    >
                      {MEETING_STATUS_LABELS[m.status]}
                    </span>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
