import { RouterProvider } from 'react-router-dom';
import { QueryProvider } from './app/providers/QueryProvider';
import { AuthProvider } from './app/providers/AuthProvider';
import { ToastProvider } from './shared/components/toast/ToastProvider';
import { ErrorBoundary } from './shared/components/ErrorBoundary';
import { router } from './routes/router';

export default function App() {
  return (
    <ErrorBoundary>
      <QueryProvider>
        <AuthProvider>
          <ToastProvider>
            <RouterProvider router={router} />
          </ToastProvider>
        </AuthProvider>
      </QueryProvider>
    </ErrorBoundary>
  );
}
