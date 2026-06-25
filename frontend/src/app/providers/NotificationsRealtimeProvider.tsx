import { useEffect, useRef, type ReactNode } from 'react';
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { tokenStorage } from '../../shared/api/tokenStorage';
import { queryKeys } from '../../shared/api/queryKeys';
import { useAuth } from './useAuth';

const HUB_URL = `${import.meta.env.VITE_API_BASE_URL?.replace('/api', '') ?? ''}/hubs/notifications`;

interface NotificationsRealtimeProviderProps {
  children: ReactNode;
}

export function NotificationsRealtimeProvider({
  children,
}: NotificationsRealtimeProviderProps) {
  const { isAuthenticated } = useAuth();
  const queryClient = useQueryClient();
  const connectionRef = useRef<ReturnType<
    typeof HubConnectionBuilder.prototype.build
  > | null>(null);

  useEffect(() => {
    if (!isAuthenticated) {
      // Stop any existing connection when the user logs out.
      if (connectionRef.current) {
        const conn = connectionRef.current;
        connectionRef.current = null;
        void conn.stop();
      }
      return;
    }

    const token = tokenStorage.getAccessToken();
    if (!token) return;

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => tokenStorage.getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connectionRef.current = connection;

    connection.on('ReceiveNotification', () => {
      void queryClient.invalidateQueries({
        queryKey: queryKeys.notifications,
      });
      void queryClient.invalidateQueries({
        queryKey: queryKeys.notificationsUnread,
      });
    });

    // Effect bu start tamamlanmadan temizlenirse (React 19 StrictMode çift-mount
    // veya hızlı navigasyon) `disposed` true olur. Bağlantıyı negotiation
    // sırasında stop() ile kesmek "connection stopped during negotiation"
    // hatasına yol açtığından, bunun yerine start tamamlanınca temiz biçimde durdururuz.
    let disposed = false;

    const startConnection = async () => {
      try {
        await connection.start();
        if (disposed) {
          // Effect negotiation sürerken temizlendi → şimdi güvenle durdur.
          await connection.stop();
        }
      } catch (err) {
        // Beklenen teardown (disposed) durumunda sessiz kal; yalnızca gerçek
        // bağlantı hatalarını logla. Polling fallback her durumda devrede.
        if (!disposed) {
          console.warn(
            '[NotificationsRealtime] SignalR hub bağlantısı kurulamadı — polling fallback aktif.',
            err instanceof Error ? err.message : err,
          );
        }
      }
    };

    void startConnection();

    return () => {
      disposed = true;
      // Yalnızca tam bağlıyken durdur; hâlâ negotiation sürüyorsa yukarıdaki
      // startConnection bağlantı kurulunca durduracak (negotiation hatası önlenir).
      if (connection.state === HubConnectionState.Connected) {
        void connection.stop();
      }
      connectionRef.current = null;
    };
  }, [isAuthenticated, queryClient]);

  return <>{children}</>;
}
