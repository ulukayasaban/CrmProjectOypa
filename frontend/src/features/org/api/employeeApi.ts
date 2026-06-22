import { httpClient } from '../../../shared/api/httpClient';
import type { EmployeeDto } from '../../../entities/employee/model/employee';

export const employeeApi = {
  async getAll(): Promise<EmployeeDto[]> {
    const { data } = await httpClient.get<EmployeeDto[]>('/employees');
    return data;
  },
};
