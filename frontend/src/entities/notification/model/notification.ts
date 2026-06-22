export interface NotificationDto {
  id: string;
  title: string | null;
  message: string;
  type: string;
  senderName: string | null;
  link: string | null;
  isRead: boolean;
  createdAtUtc: string;
}
