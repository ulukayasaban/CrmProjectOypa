import { httpClient } from '../../../shared/api/httpClient';
import type { MeetingDto } from '../../../entities/meeting/model/meeting';
import type { MeetingMethod, MeetingStatus } from '../../../shared/types/enums';

export interface MeetingPayload {
  companyId: string;
  contactId?: string;
  salesRepId: string;
  date: string;
  time: string;
  address: string;
  method: MeetingMethod;
}

export const meetingApi = {
  async getAll(): Promise<MeetingDto[]> {
    const { data } = await httpClient.get<MeetingDto[]>('/meetings');
    return data;
  },
  async create(payload: MeetingPayload): Promise<MeetingDto> {
    const { data } = await httpClient.post<MeetingDto>('/meetings', payload);
    return data;
  },
  async updateStatus(
    id: string,
    status: MeetingStatus,
    comment?: string,
  ): Promise<void> {
    await httpClient.patch(`/meetings/${id}/status`, { status, comment });
  },
  async addNote(meetingId: string, content: string): Promise<MeetingDto> {
    const { data } = await httpClient.post<MeetingDto>(
      `/meetings/${meetingId}/notes`,
      { content },
    );
    return data;
  },
};
