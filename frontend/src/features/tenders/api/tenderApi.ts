import { httpClient } from '../../../shared/api/httpClient';
import type { TenderDto, TenderStatus } from '../../../entities/tender/model/tender';
import type { Sector } from '../../../shared/types/enums';
import type { PagedParams, PagedResult } from '../../../shared/types/paged';

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

/** Paged sorgu parametreleri — /tenders/paged ucu için. */
export interface TenderPagedParams extends PagedParams {
  sector?: Sector;
  /** Durum filtresi; tek TenderStatus değeri kabul eder. */
  status?: TenderStatus;
  /** Segment için çoklu durum (sunucu tarafı filtre → doğru sayfalama/toplam). */
  statuses?: TenderStatus[];
}

export const tenderApi = {
  async getAll(params?: TenderListParams): Promise<TenderDto[]> {
    const { data } = await httpClient.get<TenderDto[]>('/tenders', { params });
    return data;
  },

  /** Sunucu tarafında arama + sıralama + sayfalama uygular. */
  async getPaged(params: TenderPagedParams): Promise<PagedResult<TenderDto>> {
    // statuses dizisini ASP.NET'in beklediği gibi tekrarlı anahtar olarak gönder
    // (statuses=A&statuses=B); axios varsayılanı bracket'lı üretir, ASP.NET bağlamaz.
    const { data } = await httpClient.get<PagedResult<TenderDto>>('/tenders/paged', {
      params,
      paramsSerializer: { indexes: null },
    });
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
