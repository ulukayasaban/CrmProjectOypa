import { httpClient } from '../../../shared/api/httpClient';
import type { NotificationDto } from '../../../entities/notification/model/notification';

export interface SendNotificationPayload {
  targetUnitId: string;
  title?: string;
  message: string;
}

/** Tek bir bildirim tür tercihi */
export interface NotificationPreferenceItem {
  type: string;
  enabled: boolean;
}

/** PUT /notifications/preferences istek gövdesi */
export interface UpdatePreferencesPayload {
  items: NotificationPreferenceItem[];
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

  /** Kendi bildirimini siler. DELETE /notifications/{id} */
  async deleteNotification(id: string): Promise<void> {
    await httpClient.delete(`/notifications/${id}`);
  },

  /** Bildirim tür tercihlerini getirir. GET /notifications/preferences */
  async getPreferences(): Promise<NotificationPreferenceItem[]> {
    const { data } =
      await httpClient.get<NotificationPreferenceItem[]>(
        '/notifications/preferences',
      );
    return data;
  },

  /** Bildirim tür tercihlerini günceller. PUT /notifications/preferences */
  async updatePreferences(
    payload: UpdatePreferencesPayload,
  ): Promise<void> {
    await httpClient.put('/notifications/preferences', payload);
  },
};
