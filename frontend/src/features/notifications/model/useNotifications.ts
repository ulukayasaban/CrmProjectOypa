import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import { notificationApi, type SendNotificationPayload } from '../api/notificationApi';

export function useNotifications(enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.notifications,
    queryFn: notificationApi.getMine,
    enabled,
  });
}

export function useUnreadCount(enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.notificationsUnread,
    queryFn: notificationApi.getUnreadCount,
    enabled,
    refetchInterval: 30_000,
  });
}

export function useMarkRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => notificationApi.markRead(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.notifications });
      void queryClient.invalidateQueries({
        queryKey: queryKeys.notificationsUnread,
      });
    },
  });
}

export function useMarkAllRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => notificationApi.markAllRead(),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.notifications });
      void queryClient.invalidateQueries({
        queryKey: queryKeys.notificationsUnread,
      });
    },
  });
}

export function useSendNotification() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SendNotificationPayload) =>
      notificationApi.send(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.notifications });
      void queryClient.invalidateQueries({
        queryKey: queryKeys.notificationsUnread,
      });
    },
  });
}

/**
 * Kendi bildirimini siler.
 * Başarı durumunda bildirim listesi ve okunmamış sayısı yenilenir.
 */
export function useDeleteNotification() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => notificationApi.deleteNotification(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.notifications });
      void queryClient.invalidateQueries({
        queryKey: queryKeys.notificationsUnread,
      });
    },
  });
}
