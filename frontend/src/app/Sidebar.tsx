import { useState } from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from './providers/useAuth';
import { useMeetings } from '../features/meetings/model/useMeetings';
import { useManagedEmployees } from '../features/employees/model/useEmployees';
import { formatDate, formatTime } from '../shared/lib/format';
import {
  CalendarIcon,
  DashboardIcon,
  MailIcon,
  SettingsIcon,
  TenderIcon,
  UsersIcon,
} from '../shared/components/icons';
import { OyakLogo } from '../shared/components/OyakLogo';

interface NavEntry {
  to: string;
  label: string;
  icon: React.ReactNode;
  adminOnly?: boolean;
  children?: NavEntry[];
}

const NAV_ITEMS: NavEntry[] = [
  { to: '/', label: 'Genel Bakış', icon: <DashboardIcon /> },
  { to: '/leads', label: 'Potansiyel Müşteriler', icon: <UsersIcon /> },
  {
    to: '/customers',
    label: 'Müşterilerimiz',
    icon: <UsersIcon />,
    children: [
      { to: '/customers/aktif', label: 'Aktif Müşteriler', icon: <UsersIcon /> },
      { to: '/customers/pasif', label: 'Pasif Müşteriler', icon: <UsersIcon /> },
    ],
  },
  {
    to: '/tenders',
    label: 'İhaleler',
    icon: <TenderIcon />,
    children: [
      { to: '/tenders/aktif', label: 'Aktif İhaleler', icon: <TenderIcon /> },
      { to: '/tenders/kazanilan', label: 'Kazanılan', icon: <TenderIcon /> },
      { to: '/tenders/kaybedilen', label: 'Kaybedilen', icon: <TenderIcon /> },
    ],
  },
  { to: '/calendar', label: 'Ziyaret Takvimi', icon: <CalendarIcon /> },
  { to: '/gorusme-gecmisi', label: 'Görüşme Geçmişi', icon: <CalendarIcon /> },
  { to: '/mail-drafts', label: 'Mail Taslakları', icon: <MailIcon /> },
  { to: '/raporlar', label: 'Raporlar', icon: <SettingsIcon /> },
  { to: '/management', label: 'Yönetim', icon: <SettingsIcon />, adminOnly: true },
  { to: '/organizasyon', label: 'Organizasyon', icon: <UsersIcon /> },
  { to: '/personel-yonetimi', label: 'Personel Yönetimi', icon: <SettingsIcon /> },
  { to: '/hedefler', label: 'Hedefler', icon: <SettingsIcon /> },
];

/**
 * `item.children` alanının var olup olmadığını runtime'da kontrol eden tip guard.
 * `as` cast yerine yapısal kontrol kullanılır.
 */
function hasChildren(
  item: NavEntry,
): item is NavEntry & { children: NavEntry[] } {
  return Array.isArray(item.children) && item.children.length > 0;
}

function isChildActive(children: NavEntry[], pathname: string): boolean {
  return children.some(
    (child) => pathname === child.to || pathname.startsWith(child.to + '/'),
  );
}

interface ExpandableNavGroupProps {
  item: NavEntry & { children: NavEntry[] };
}

function ExpandableNavGroup({ item }: ExpandableNavGroupProps) {
  const { pathname } = useLocation();
  const childActive = isChildActive(item.children, pathname);
  const [open, setOpen] = useState(childActive);

  return (
    <div className="nav-group">
      <button
        type="button"
        className={`nav-item nav-item--group${childActive ? ' active' : ''}`}
        onClick={() => setOpen((prev) => !prev)}
        aria-expanded={open}
      >
        {item.icon}
        <span style={{ flex: 1, textAlign: 'left' }}>{item.label}</span>
        <span style={{ fontSize: '0.7rem', marginLeft: 4 }}>{open ? '▾' : '▸'}</span>
      </button>
      {open && (
        <div className="nav-group__children">
          {item.children.map((child) => (
            <NavLink
              key={child.to}
              to={child.to}
              className={({ isActive }) =>
                `nav-item nav-item--child${isActive ? ' active' : ''}`
              }
            >
              <span style={{ paddingLeft: 8 }}>{child.label}</span>
            </NavLink>
          ))}
        </div>
      )}
    </div>
  );
}

interface SidebarProps {
  /** Mobil off-canvas çekmece açık mı (yalnız dar ekranda etkili). */
  open?: boolean;
}

export function Sidebar({ open = false }: SidebarProps) {
  const navigate = useNavigate();
  const { hasRole } = useAuth();
  const meetings = useMeetings();
  const managedEmployees = useManagedEmployees();

  const items = NAV_ITEMS.filter((item) => {
    if (item.to === '/personel-yonetimi' || item.to === '/hedefler') {
      return hasRole('Admin') || (managedEmployees.data?.length ?? 0) > 0;
    }
    return !item.adminOnly || hasRole('Admin');
  });

  // Yerel gün anahtarı (toISOString UTC döndürür → gece yarısı–03:00 arası yanlış gün verirdi).
  const now = new Date();
  const todayKey = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
  const upcoming = (meetings.data ?? [])
    .filter(
      (meeting) =>
        meeting.status === 'Planned' && meeting.date.slice(0, 10) >= todayKey,
    )
    .sort((a, b) =>
      `${a.date}T${a.time}`.localeCompare(`${b.date}T${b.time}`),
    )
    .slice(0, 3);

  return (
    <aside className={`sidebar glass${open ? ' sidebar--open' : ''}`}>
      <div
        className="logo"
        role="button"
        tabIndex={0}
        aria-label="Ana sayfaya git"
        onClick={() => navigate('/')}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') navigate('/');
        }}
      >
        <OyakLogo height={32} />
      </div>
      <nav className="nav-menu">
        {items.map((item) => {
          if (hasChildren(item)) {
            return (
              <ExpandableNavGroup
                key={item.to}
                item={item}
              />
            );
          }
          return (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) =>
                `nav-item ${isActive ? 'active' : ''}`
              }
            >
              {item.icon}
              <span>{item.label}</span>
            </NavLink>
          );
        })}
      </nav>
      <div
        className="side-module glass"
        style={{ marginTop: 'auto', padding: 15, borderRadius: 12 }}
      >
        <h4
          style={{
            fontSize: '0.7rem',
            marginBottom: 10,
            color: 'var(--accent-gold)',
            textTransform: 'uppercase',
          }}
        >
          Yaklaşanlar
        </h4>
        {upcoming.length === 0 ? (
          <p className="muted" style={{ fontSize: '0.7rem' }}>
            Planlanmış etkinlik yok.
          </p>
        ) : (
          <div className="upcoming-list">
            {upcoming.map((meeting) => (
              <div
                key={meeting.id}
                className="upcoming-item"
                role="button"
                tabIndex={0}
                aria-label={`${meeting.companyTitle} randevusuna git`}
                onClick={() => navigate('/calendar')}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') navigate('/calendar');
                }}
              >
                <strong>{meeting.companyTitle}</strong>
                <span className="muted">
                  {formatDate(meeting.date)} · {formatTime(meeting.time)}
                </span>
              </div>
            ))}
          </div>
        )}
      </div>
    </aside>
  );
}
