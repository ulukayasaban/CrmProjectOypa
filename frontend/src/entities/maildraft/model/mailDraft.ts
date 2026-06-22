export interface MailDraftDto {
  id: string;
  to: string;
  cc: string | null;
  subject: string;
  body: string;
  sent: boolean;
  sentAtUtc: string | null;
  createdAtUtc: string;
  meetingId: string | null;
}
