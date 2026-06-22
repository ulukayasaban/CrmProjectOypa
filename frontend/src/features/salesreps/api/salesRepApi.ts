import { httpClient } from '../../../shared/api/httpClient';
import type { SalesRepDto } from '../../../entities/salesrep/model/salesRep';

export interface SalesRepPayload {
  name: string;
  email: string;
}

export const salesRepApi = {
  async getAll(): Promise<SalesRepDto[]> {
    const { data } = await httpClient.get<SalesRepDto[]>('/salesreps');
    return data;
  },
  async create(payload: SalesRepPayload): Promise<SalesRepDto> {
    const { data } = await httpClient.post<SalesRepDto>('/salesreps', payload);
    return data;
  },
  async linkEmployee(id: string, employeeId: string | null): Promise<void> {
    await httpClient.patch(`/salesreps/${id}/employee`, { employeeId });
  },
};
