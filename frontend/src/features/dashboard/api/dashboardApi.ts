import { httpClient } from '../../../shared/api/httpClient';
import type { DashboardDto } from '../../../entities/dashboard/model/dashboard';

export const dashboardApi = {
  async get(): Promise<DashboardDto> {
    const { data } = await httpClient.get<DashboardDto>('/dashboard');
    return data;
  },
};
