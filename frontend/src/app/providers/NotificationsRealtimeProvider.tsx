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

    const startConnection = async () => {
      try {
        await connection.start();
      } catch {
        // Connection failures are non-fatal; polling acts as fallback.
      }
    };

    void startConnection();

    return () => {
      if (
        connection.state !== HubConnectionState.Disconnected &&
        connection.state !== HubConnectionState.Disconnecting
      ) {
        void connection.stop();
      }
      connectionRef.current = null;
    };
  }, [isAuthenticated, queryClient]);

  return <>{children}</>;
}
