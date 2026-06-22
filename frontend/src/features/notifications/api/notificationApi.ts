import { httpClient } from '../../../shared/api/httpClient';
import type { NotificationDto } from '../../../entities/notification/model/notification';

export interface SendNotificationPayload {
  targetUnitId: string;
  title?: string;
  message: string;
}

export const notificationApi = {
  async getMine(): Promise<NotificationDto[]> {
    const { data } = await httpClient.get<NotificationDto[]>('/notifications');
    return data;
  },

  async getUnreadCount(): Promise<number> {
    const { data } = await httpClient.get<number>(
      '/notifications/unread-count',
    );
    return data;
  },

  async markRead(id: string): Promise<void> {
    await httpClient.post(`/notifications/${id}/read`);
  },

  async markAllRead(): Promise<void> {
    await httpClient.post('/notifications/mark-all-read');
  },

  async send(payload: SendNotificationPayload): Promise<void> {
    await httpClient.post('/notifications/send', payload);
  },
};
