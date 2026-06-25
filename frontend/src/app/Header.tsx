import { useNavigate } from 'react-router-dom';
import { useAuth } from './providers/useAuth';
import { NotificationBell } from '../features/notifications/ui/NotificationBell';

interface HeaderProps {
  title: string;
  onMenuToggle?: () => void;
}

export function Header({ title, onMenuToggle }: HeaderProps) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const initial = user?.fullName?.[0] ?? '?';

  return (
    <header className="main-header glass">
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, minWidth: 0 }}>
        <button
          type="button"
          className="nav-toggle"
          onClick={onMenuToggle}
          aria-label="Menüyü aç/kapat"
        >
          ☰
        </button>
        <h2 className="header-title" style={{ fontSize: '1.1rem', color: 'var(--text-muted)' }}>{title}</h2>
      </div>
      <div className="header-right">
        <NotificationBell />
        <div
          className="user-profile"
          onClick={() => navigate('/profile')}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 10,
            cursor: 'pointer',
          }}
        >
          <div className="user-meta" style={{ textAlign: 'right', lineHeight: 1.2 }}>
            <div style={{ fontSize: '0.85rem', fontWeight: 600 }}>
              {user?.fullName}
            </div>
            <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>
              {user?.position ?? user?.roles.join(', ')}
            </div>
          </div>
          <div className="avatar">{initial}</div>
        </div>
        <button
          type="button"
          className="btn btn-ghost btn-sm"
          onClick={() => {
            void logout();
          }}
        >
          Çıkış
        </button>
      </div>
    </header>
  );
}
