import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../app/providers/useAuth';
import { Spinner } from '../shared/components/Spinner';

export function ProtectedRoute() {
  const { isAuthenticated, isInitializing } = useAuth();
  const location = useLocation();

  if (isInitializing) {
    return <Spinner />;
  }

  if (!isAuthenticated) {
    return (
      <Navigate to="/login" replace state={{ from: location.pathname }} />
    );
  }

  return <Outlet />;
}
