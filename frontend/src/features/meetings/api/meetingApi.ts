import { httpClient } from '../../../shared/api/httpClient';
import type { MeetingDto } from '../../../entities/meeting/model/meeting';
import type { MeetingMethod, MeetingStatus } from '../../../shared/types/enums';
import type { PagedParams, PagedResult } from '../../../shared/types/paged';

export interface MeetingPayload {
  companyId: string;
  contactId?: string;
  salesRepId: string;
  date: string;
  time: string;
  address: string;
  method: MeetingMethod;
}

/** /meetings/paged ucu için parametre tipi. */
export type MeetingPagedParams = PagedParams;

export const meetingApi = {
  async getAll(): Promise<MeetingDto[]> {
    const { data } = await httpClient.get<MeetingDto[]>('/meetings');
    return data;
  },

  /** Sunucu taraflı sayfalı görüşme listesi. */
  async getPaged(params: MeetingPagedParams): Promise<PagedResult<MeetingDto>> {
    const { data } = await httpClient.get<PagedResult<MeetingDto>>('/meetings/paged', { params });
    return data;
  },
  async create(payload: MeetingPayload): Promise<MeetingDto> {
    const { data } = await httpClient.post<MeetingDto>('/meetings', payload);
    return data;
  },

  /** Mevcut görüşmeyi günceller. PUT /meetings/{id} */
  async update(id: string, payload: MeetingPayload): Promise<MeetingDto> {
    const { data } = await httpClient.put<MeetingDto>(`/meetings/${id}`, payload);
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
