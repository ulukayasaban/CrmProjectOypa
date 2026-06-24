import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  useDeleteNotification,
  useMarkAllRead,
  useMarkRead,
  useNotifications,
  useUnreadCount,
} from '../model/useNotifications';
import { formatDateTime } from '../../../shared/lib/format';
import { useAuth } from '../../../app/providers/useAuth';
import { useManagedEmployees } from '../../employees/model/useEmployees';
import { NotificationComposeModal } from './NotificationComposeModal';
import { NotificationPreferencesModal } from './NotificationPreferencesModal';
import { getErrorMessage } from '../../../shared/lib/errorMessage';
import type { NotificationDto } from '../../../entities/notification/model/notification';

function notificationTypeLabel(type: string): string {
  switch (type) {
    case 'Manual':
      return 'Bildirim';
    case 'MeetingScheduled':
      return 'Görüşme Planlandı';
    case 'MeetingNoteAdded':
      return 'Not Eklendi';
    case 'GoalAssigned':
      return 'Hedef Atandı';
    case 'LeadConverted':
      return 'Müşteriye Dönüştü';
    default:
      return type;
  }
}

interface NotificationItemProps {
  item: NotificationDto;
  onRead: (id: string) => void;
  onDelete: (id: string) => void;
}

function NotificationItem({ item, onRead, onDelete }: NotificationItemProps) {
  const navigate = useNavigate();

  function handleClick() {
    if (!item.isRead) {
      onRead(item.id);
    }
    if (item.link) {
      navigate(item.link);
    }
  }

  return (
    <div
      className={`notification-item ${item.isRead ? 'read' : 'unread'}`}
      onClick={handleClick}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') handleClick();
      }}
      style={{ cursor: item.link || !item.isRead ? 'pointer' : 'default', position: 'relative' }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', gap: 8 }}>
        <span style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>
          {notificationTypeLabel(item.type)}
        </span>
        <div style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
          {item.senderName && (
            <span style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>
              {item.senderName}
            </span>
          )}
          {/* Sil butonu — item'ın navigate davranışını tetiklememek için stopPropagation */}
          <button
            type="button"
            aria-label="Bildirimi sil"
            style={{
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              padding: '0 2px',
              lineHeight: 1,
              color: 'var(--text-muted)',
              fontSize: '0.85rem',
            }}
            onClick={(e) => {
              e.stopPropagation(); // navigate / read tetikleme
              onDelete(item.id);
            }}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.stopPropagation();
                onDelete(item.id);
              }
            }}
          >
            ×
          </button>
        </div>
      </div>
      {item.title && (
        <div style={{ fontWeight: 600, fontSize: '0.85rem' }}>{item.title}</div>
      )}
      <div>{item.message}</div>
      <div className="muted" style={{ fontSize: '0.7rem' }}>
        {formatDateTime(item.createdAtUtc)}
      </div>
    </div>
  );
}

export function NotificationBell() {
  const [open, setOpen] = useState(false);
  const [composeOpen, setComposeOpen] = useState(false);
  const [prefsOpen, setPrefsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const { hasRole } = useAuth();
  const managedEmployees = useManagedEmployees();

  const unreadCount = useUnreadCount(true);
  const notifications = useNotifications(open);
  const markRead = useMarkRead();
  const markAllRead = useMarkAllRead();
  const deleteNotification = useDeleteNotification();

  // Gönderme yetkisi = Admin VEYA en az bir astı olan yönetici.
  // GetManagedAsync kapsamı kullanıcının kendisini de içerir; bu yüzden > 1
  // (yalnız kendisi olan Sales kullanıcıları hariç tutulur — backend de 403 döner).
  const canCompose =
    hasRole('Admin') || (managedEmployees.data?.length ?? 0) > 1;

  useEffect(() => {
    if (!open) return;
    function onClickOutside(event: MouseEvent) {
      if (
        containerRef.current &&
        !containerRef.current.contains(event.target as Node)
      ) {
        setOpen(false);
      }
    }
    document.addEventListener('mousedown', onClickOutside);
    return () => document.removeEventListener('mousedown', onClickOutside);
  }, [open]);

  const count = unreadCount.data ?? 0;

  return (
    <>
      <div ref={containerRef} style={{ position: 'relative' }}>
        <button
          type="button"
          className="notification-bell"
          onClick={() => setOpen((value) => !value)}
          aria-label="Bildirimler"
        >
          🔔
          {count > 0 && <span className="badge-count">{count}</span>}
        </button>
        {open && (
          <div className="notification-popover glass">
            <div className="notification-popover-head">
              <h4>Bildirimler</h4>
              <div className="head-actions">
                {/* Bildirim tür tercihleri ayar butonu */}
                <button
                  type="button"
                  className="btn btn-ghost btn-sm"
                  aria-label="Bildirim tercihlerini ayarla"
                  onClick={() => {
                    setOpen(false);
                    setPrefsOpen(true);
                  }}
                >
                  ⚙ Tercihler
                </button>
                {canCompose && (
                  <button
                    type="button"
                    className="btn btn-ghost btn-sm"
                    onClick={() => {
                      setOpen(false);
                      setComposeOpen(true);
                    }}
                  >
                    Bildirim Gönder
                  </button>
                )}
                <button
                  type="button"
                  className="btn btn-ghost btn-sm"
                  onClick={() => markAllRead.mutate()}
                  disabled={markAllRead.isPending || count === 0}
                >
                  Tümünü Okundu Yap
                </button>
              </div>
            </div>
            {/* markAllRead hata geri bildirimi — popover içinde küçük hata satırı */}
            {markAllRead.isError && (
              <p className="form-error" style={{ margin: '4px 0', fontSize: '0.78rem' }}>
                {getErrorMessage(markAllRead.error)}
              </p>
            )}
            {notifications.isLoading && <p className="muted">Yükleniyor...</p>}
            {notifications.data && notifications.data.length === 0 && (
              <p className="muted">Bildirim yok.</p>
            )}
            {(notifications.data ?? []).map((item) => (
              <NotificationItem
                key={item.id}
                item={item}
                onRead={(id) => markRead.mutate(id)}
                onDelete={(id) => deleteNotification.mutate(id)}
              />
            ))}
          </div>
        )}
      </div>

      {composeOpen && (
        <NotificationComposeModal onClose={() => setComposeOpen(false)} />
      )}

      {prefsOpen && (
        <NotificationPreferencesModal onClose={() => setPrefsOpen(false)} />
      )}
    </>
  );
}
