import { httpClient } from '../../../shared/api/httpClient';
import type { TenderDto, TenderStatus } from '../../../entities/tender/model/tender';
import type { Sector } from '../../../shared/types/enums';

export interface TenderPayload {
  companyId: string;
  title: string;
  tenderNumber?: string | null;
  sector: Sector;
  tenderDate: string;
  status?: TenderStatus;
  personnelCount?: number | null;
  estimatedValue?: number | null;
  volume?: number | null;
  quantity?: number | null;
  description?: string | null;
  assignedSalesRepId?: string | null;
}

export interface TenderListParams {
  sector?: Sector;
  status?: TenderStatus;
}

export const tenderApi = {
  async getAll(params?: TenderListParams): Promise<TenderDto[]> {
    const { data } = await httpClient.get<TenderDto[]>('/tenders', { params });
    return data;
  },
  async getById(id: string): Promise<TenderDto> {
    const { data } = await httpClient.get<TenderDto>(`/tenders/${id}`);
    return data;
  },
  async create(payload: TenderPayload): Promise<TenderDto> {
    const { data } = await httpClient.post<TenderDto>('/tenders', payload);
    return data;
  },
  async update(id: string, payload: TenderPayload): Promise<TenderDto> {
    const { data } = await httpClient.put<TenderDto>(`/tenders/${id}`, payload);
    return data;
  },
  async changeStatus(id: string, status: TenderStatus): Promise<void> {
    await httpClient.patch(`/tenders/${id}/status`, { status });
  },
  async remove(id: string): Promise<void> {
    await httpClient.delete(`/tenders/${id}`);
  },
};
