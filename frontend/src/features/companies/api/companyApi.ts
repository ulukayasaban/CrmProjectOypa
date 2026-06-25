import { httpClient } from '../../../shared/api/httpClient';
import type {
  CompanyDto,
  ContactDto,
} from '../../../entities/company/model/company';
import type { MeetingDto } from '../../../entities/meeting/model/meeting';
import type {
  CompanySource,
  CustomerStatus,
  LeadStatus,
  Sector,
} from '../../../shared/types/enums';
import type { PagedParams, PagedResult } from '../../../shared/types/paged';

export interface CompanyPayload {
  title: string;
  sector: Sector;
  phone: string;
  email: string;
  address: string;
  city?: string;
  website?: string;
  taxNumber?: string;
  source?: CompanySource;
}

/** /companies/leads/paged ucu için parametre tipi. */
export interface LeadsPagedParams extends PagedParams {
  status?: LeadStatus;
  categoryId?: string;
}

/** /companies/customers/paged ucu için parametre tipi. */
export interface CustomersPagedParams extends PagedParams {
  status?: CustomerStatus;
  categoryId?: string;
}

export interface ContactPayload {
  name: string;
  email?: string;
  phone?: string;
}

export const companyApi = {
  async getLeads(status?: LeadStatus): Promise<CompanyDto[]> {
    const params = status ? { status } : {};
    const { data } = await httpClient.get<CompanyDto[]>('/companies/leads', {
      params,
    });
    return data;
  },
  async getCustomers(status?: CustomerStatus): Promise<CompanyDto[]> {
    const params = status ? { status } : {};
    const { data } = await httpClient.get<CompanyDto[]>('/companies/customers', {
      params,
    });
    return data;
  },

  /** Sunucu taraflı sayfalı lead listesi. */
  async getLeadsPaged(params: LeadsPagedParams): Promise<PagedResult<CompanyDto>> {
    const { data } = await httpClient.get<PagedResult<CompanyDto>>('/companies/leads/paged', { params });
    return data;
  },

  /** Sunucu taraflı sayfalı müşteri listesi. */
  async getCustomersPaged(params: CustomersPagedParams): Promise<PagedResult<CompanyDto>> {
    const { data } = await httpClient.get<PagedResult<CompanyDto>>('/companies/customers/paged', { params });
    return data;
  },
  async getById(id: string): Promise<CompanyDto> {
    const { data } = await httpClient.get<CompanyDto>(`/companies/${id}`);
    return data;
  },
  async create(payload: CompanyPayload): Promise<CompanyDto> {
    const { data } = await httpClient.post<CompanyDto>('/companies', payload);
    return data;
  },
  async update(id: string, payload: CompanyPayload): Promise<CompanyDto> {
    const { data } = await httpClient.put<CompanyDto>(
      `/companies/${id}`,
      payload,
    );
    return data;
  },
  async updateLeadStatus(id: string, status: LeadStatus): Promise<void> {
    await httpClient.patch(`/companies/${id}/lead-status`, { status });
  },
  async updateCustomerStatus(
    id: string,
    status: CustomerStatus,
  ): Promise<void> {
    await httpClient.patch(`/companies/${id}/customer-status`, { status });
  },
  async convert(id: string): Promise<CompanyDto> {
    const { data } = await httpClient.post<CompanyDto>(
      `/companies/${id}/convert`,
    );
    return data;
  },
  async getContacts(id: string): Promise<ContactDto[]> {
    const { data } = await httpClient.get<ContactDto[]>(
      `/companies/${id}/contacts`,
    );
    return data;
  },
  async createContact(
    id: string,
    payload: ContactPayload,
  ): Promise<ContactDto> {
    const { data } = await httpClient.post<ContactDto>(
      `/companies/${id}/contacts`,
      payload,
    );
    return data;
  },

  /** İlgili kişiyi günceller. PUT /contacts/{contactId} */
  async updateContact(
    contactId: string,
    payload: ContactPayload,
  ): Promise<ContactDto> {
    const { data } = await httpClient.put<ContactDto>(
      `/contacts/${contactId}`,
      payload,
    );
    return data;
  },

  /** İlgili kişiyi siler. DELETE /contacts/{contactId} */
  async deleteContact(contactId: string): Promise<void> {
    await httpClient.delete(`/contacts/${contactId}`);
  },
  async getMeetings(id: string): Promise<MeetingDto[]> {
    const { data } = await httpClient.get<MeetingDto[]>(
      `/companies/${id}/meetings`,
    );
    return data;
  },
  async assignSalesRep(
    id: string,
    salesRepId: string | null,
  ): Promise<void> {
    await httpClient.patch(`/companies/${id}/assign-rep`, { salesRepId });
  },
};
