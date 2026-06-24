import { Suspense, useEffect, useState } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Header } from './Header';
import { NotificationsRealtimeProvider } from './providers/NotificationsRealtimeProvider';
import { Spinner } from '../shared/components/Spinner';

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
  // Mobil off-canvas sidebar durumu. Rota değişince otomatik kapanır.
  const [navOpen, setNavOpen] = useState(false);

  // Rota değişince mobil çekmeceyi kapat. Effect içinde setState bilinçli ve güvenlidir:
  // yalnızca pathname değiştiğinde tetiklenir, döngü oluşturmaz.
  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setNavOpen(false);
  }, [location.pathname]);

  return (
    <NotificationsRealtimeProvider>
      <div className="app-container">
        <Sidebar open={navOpen} />
        {navOpen && (
          <div
            className="sidebar-overlay"
            role="presentation"
            onClick={() => setNavOpen(false)}
          />
        )}
        <main className="main-content">
          <Header
            title={resolveTitle(location.pathname)}
            onMenuToggle={() => setNavOpen((prev) => !prev)}
          />
          {/* Lazy yüklenen sayfa bileşenleri için Suspense fallback */}
          <Suspense fallback={<Spinner />}>
            <div className="view-container">
              <Outlet />
            </div>
          </Suspense>
        </main>
      </div>
    </NotificationsRealtimeProvider>
  );
}
