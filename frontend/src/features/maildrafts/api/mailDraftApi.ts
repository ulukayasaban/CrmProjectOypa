import { httpClient } from '../../../shared/api/httpClient';
import type { MailDraftDto } from '../../../entities/maildraft/model/mailDraft';

export const mailDraftApi = {
  async getAll(): Promise<MailDraftDto[]> {
    const { data } = await httpClient.get<MailDraftDto[]>('/maildrafts');
    return data;
  },
  async send(id: string): Promise<void> {
    await httpClient.post(`/maildrafts/${id}/send`);
  },
  async getByMeeting(meetingId: string): Promise<MailDraftDto> {
    const { data } = await httpClient.get<MailDraftDto>(
      `/maildrafts/by-meeting/${meetingId}`,
    );
    return data;
  },
  async downloadEml(id: string): Promise<Blob> {
    const res = await httpClient.get(`/maildrafts/${id}/eml`, {
      responseType: 'blob',
    });
    return res.data as Blob;
  },
};
