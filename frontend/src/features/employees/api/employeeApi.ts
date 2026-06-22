import { httpClient } from '../../../shared/api/httpClient';
import type { EmployeeDto } from '../../../entities/employee/model/employee';

export interface CreateEmployeePayload {
  title: string;
  fullName?: string;
  email?: string;
  managerId?: string;
  createAccount: boolean;
  role?: string;
}

export interface UpdateEmployeePayload {
  title: string;
  fullName?: string;
  email?: string;
}

export interface AssignManagerPayload {
  managerId: string | null;
}

export interface CreateAccountPayload {
  role: string;
}

export interface AssignRolePayload {
  role: string;
}

export interface AccountCredentials {
  email: string;
  tempPassword: string;
}

export interface CreateEmployeeResult {
  employee: EmployeeDto;
  account: AccountCredentials | null;
}

export const employeeApi = {
  async getManaged(): Promise<EmployeeDto[]> {
    const { data } = await httpClient.get<EmployeeDto[]>('/employees/managed');
    return data;
  },

  async create(payload: CreateEmployeePayload): Promise<CreateEmployeeResult> {
    const { data } = await httpClient.post<CreateEmployeeResult>('/employees', payload);
    return data;
  },

  async update(id: string, payload: UpdateEmployeePayload): Promise<EmployeeDto> {
    const { data } = await httpClient.put<EmployeeDto>(`/employees/${id}`, payload);
    return data;
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/employees/${id}`);
  },

  async assignManager(id: string, payload: AssignManagerPayload): Promise<void> {
    await httpClient.put(`/employees/${id}/manager`, payload);
  },

  async createAccount(id: string, payload: CreateAccountPayload): Promise<AccountCredentials> {
    const { data } = await httpClient.post<AccountCredentials>(`/employees/${id}/account`, payload);
    return data;
  },

  async assignRole(id: string, payload: AssignRolePayload): Promise<void> {
    await httpClient.put(`/employees/${id}/role`, payload);
  },

  async resetPassword(id: string): Promise<AccountCredentials> {
    const { data } = await httpClient.post<AccountCredentials>(`/employees/${id}/reset-password`);
    return data;
  },

  async unlinkAccount(id: string): Promise<void> {
    await httpClient.delete(`/employees/${id}/account`);
  },
};
