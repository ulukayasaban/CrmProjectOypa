import { Outlet, useLocation } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Header } from './Header';
import { NotificationsRealtimeProvider } from './providers/NotificationsRealtimeProvider';

function resolveTitle(pathname: string): string {
  if (pathname === '/') return 'Genel Bakış';
  if (pathname.startsWith('/leads')) return 'Potansiyel Müşteriler';
  if (pathname === '/customers/aktif') return 'Aktif Müşteriler';
  if (pathname === '/customers/pasif') return 'Pasif Müşteriler';
  if (pathname.startsWith('/customers')) return 'Müşterilerimiz';
  if (pathname.startsWith('/companies')) return 'Firma Detayı';
  if (pathname.startsWith('/calendar')) return 'Ziyaret Takvimi';
  if (pathname.startsWith('/gorusme-gecmisi')) return 'Görüşme Geçmişi';
  if (pathname.startsWith('/mail-drafts')) return 'Mail Taslakları';
  if (pathname.startsWith('/raporlar')) return 'Raporlar';
  if (pathname.startsWith('/management')) return 'Yönetim';
  if (pathname.startsWith('/organizasyon')) return 'Organizasyon';
  if (pathname.startsWith('/personel-yonetimi')) return 'Personel Yönetimi';
  if (pathname.startsWith('/hedefler')) return 'Hedefler';
  if (pathname.startsWith('/profile')) return 'Profilim';
  if (pathname.startsWith('/tenders/detay')) return 'İhale Detayı';
  if (pathname === '/tenders/aktif') return 'Aktif İhaleler';
  if (pathname === '/tenders/kazanilan') return 'Kazanılan İhaleler';
  if (pathname === '/tenders/kaybedilen') return 'Kaybedilen İhaleler';
  if (pathname.startsWith('/tenders')) return 'İhaleler';
  return 'OYPA CRM';
}

export function AppLayout() {
  const location = useLocation();

  return (
    <NotificationsRealtimeProvider>
      <div className="app-container">
        <Sidebar />
        <main className="main-content">
          <Header title={resolveTitle(location.pathname)} />
          <div className="view-container">
            <Outlet />
          </div>
        </main>
      </div>
    </NotificationsRealtimeProvider>
  );
}
