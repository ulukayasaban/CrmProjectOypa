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

export function Sidebar() {
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

  const todayKey = new Date().toISOString().slice(0, 10);
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
    <aside className="sidebar glass">
      <div className="logo" onClick={() => navigate('/')}>
        <div className="logo-mark">O</div>
        <h1>
          OYPA<span>CRM</span>
        </h1>
      </div>
      <nav className="nav-menu">
        {items.map((item) => {
          if (item.children && item.children.length > 0) {
            return (
              <ExpandableNavGroup
                key={item.to}
                item={item as NavEntry & { children: NavEntry[] }}
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
                onClick={() => navigate('/calendar')}
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
