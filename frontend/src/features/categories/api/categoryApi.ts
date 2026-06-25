import { httpClient } from '../../../shared/api/httpClient';
import type { CategoryDto } from '../../../entities/category/model/category';
import type { CompanyDto } from '../../../entities/company/model/company';

export interface CategoryPayload {
  name: string;
  color: string;
}

export const categoryApi = {
  async getAll(): Promise<CategoryDto[]> {
    const { data } = await httpClient.get<CategoryDto[]>('/categories');
    return data;
  },

  async create(payload: CategoryPayload): Promise<CategoryDto> {
    const { data } = await httpClient.post<CategoryDto>('/categories', payload);
    return data;
  },

  async update(id: string, payload: CategoryPayload): Promise<CategoryDto> {
    const { data } = await httpClient.put<CategoryDto>(`/categories/${id}`, payload);
    return data;
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/categories/${id}`);
  },

  /** Firmaya kategori ataması — PUT /companies/{id}/categories */
  async setCompanyCategories(companyId: string, categoryIds: string[]): Promise<CompanyDto> {
    const { data } = await httpClient.put<CompanyDto>(
      `/companies/${companyId}/categories`,
      { categoryIds },
    );
    return data;
  },
};
