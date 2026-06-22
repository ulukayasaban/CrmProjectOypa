import { createBrowserRouter, Navigate } from 'react-router-dom';
import { lazy } from 'react';
import { AppLayout } from '../app/AppLayout';
import { ProtectedRoute } from './ProtectedRoute';
import LoginPage from '../pages/LoginPage';
import DashboardPage from '../pages/DashboardPage';
import LeadsPage from '../pages/LeadsPage';
import CustomersPage from '../pages/CustomersPage';
import CompanyDetailPage from '../pages/CompanyDetailPage';
import CalendarPage from '../pages/CalendarPage';
import MailDraftsPage from '../pages/MailDraftsPage';
import ProfilePage from '../pages/ProfilePage';
import ManagementPage from '../pages/ManagementPage';
import OrgChartPage from '../pages/OrgChartPage';
import EmployeeManagementPage from '../pages/EmployeeManagementPage';
import MeetingHistoryPage from '../pages/MeetingHistoryPage';
import ReportsPage from '../pages/ReportsPage';

const GoalsPage = lazy(() => import('../pages/GoalsPage'));
const TendersPage = lazy(() => import('../pages/TendersPage'));
const TenderDetailPage = lazy(() => import('../pages/TenderDetailPage'));

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
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
