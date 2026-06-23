import { createBrowserRouter, Navigate } from 'react-router-dom';
import { lazy, Suspense } from 'react';
import { AppLayout } from '../app/AppLayout';
import { ProtectedRoute } from './ProtectedRoute';
import { Spinner } from '../shared/components/Spinner';

// Tüm sayfa bileşenleri lazy import ile code-splitting uygulanır.
// Suspense fallback AppLayout içinde Outlet'i sarar.
const LoginPage = lazy(() => import('../pages/LoginPage'));
const DashboardPage = lazy(() => import('../pages/DashboardPage'));
const LeadsPage = lazy(() => import('../pages/LeadsPage'));
const CustomersPage = lazy(() => import('../pages/CustomersPage'));
const CompanyDetailPage = lazy(() => import('../pages/CompanyDetailPage'));
const CalendarPage = lazy(() => import('../pages/CalendarPage'));
const MailDraftsPage = lazy(() => import('../pages/MailDraftsPage'));
const ProfilePage = lazy(() => import('../pages/ProfilePage'));
const ManagementPage = lazy(() => import('../pages/ManagementPage'));
const OrgChartPage = lazy(() => import('../pages/OrgChartPage'));
const EmployeeManagementPage = lazy(() => import('../pages/EmployeeManagementPage'));
const MeetingHistoryPage = lazy(() => import('../pages/MeetingHistoryPage'));
const ReportsPage = lazy(() => import('../pages/ReportsPage'));
const GoalsPage = lazy(() => import('../pages/GoalsPage'));
const TendersPage = lazy(() => import('../pages/TendersPage'));
const TenderDetailPage = lazy(() => import('../pages/TenderDetailPage'));

export const router = createBrowserRouter([
  {
    path: '/login',
    // LoginPage AppLayout dışında olduğu için kendi Suspense sargısını alır.
    element: (
      <Suspense fallback={<Spinner />}>
        <LoginPage />
      </Suspense>
    ),
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppLayout />,
        children: [
          { index: true, element: <DashboardPage /> },
          { path: 'leads', element: <LeadsPage /> },
          // /customers → redirect to /customers/aktif
          { path: 'customers', element: <Navigate to="/customers/aktif" replace /> },
          { path: 'customers/:segment', element: <CustomersPage /> },
          { path: 'companies/:id', element: <CompanyDetailPage /> },
          { path: 'calendar', element: <CalendarPage /> },
          { path: 'gorusme-gecmisi', element: <MeetingHistoryPage /> },
          { path: 'mail-drafts', element: <MailDraftsPage /> },
          { path: 'raporlar', element: <ReportsPage /> },
          { path: 'management', element: <ManagementPage /> },
          { path: 'organizasyon', element: <OrgChartPage /> },
          { path: 'personel-yonetimi', element: <EmployeeManagementPage /> },
          { path: 'hedefler', element: <GoalsPage /> },
          { path: 'profile', element: <ProfilePage /> },
          // /tenders → redirect to /tenders/aktif
          { path: 'tenders', element: <Navigate to="/tenders/aktif" replace /> },
          { path: 'tenders/detay/:id', element: <TenderDetailPage /> },
          { path: 'tenders/:segment', element: <TendersPage /> },
        ],
      },
    ],
  },
  {
    path: '*',
    element: <Navigate to="/" replace />,
  },
]);
