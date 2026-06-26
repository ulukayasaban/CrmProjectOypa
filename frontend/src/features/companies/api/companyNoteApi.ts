import { httpClient } from '../../../shared/api/httpClient';
import type { CompanyNoteDto } from '../../../entities/company/model/companyNote';

export const companyNoteApi = {
  async list(companyId: string): Promise<CompanyNoteDto[]> {
    const { data } = await httpClient.get<CompanyNoteDto[]>(
      `/companies/${companyId}/notes`,
    );
    return data;
  },

  async create(companyId: string, content: string): Promise<CompanyNoteDto> {
    const { data } = await httpClient.post<CompanyNoteDto>(
      `/companies/${companyId}/notes`,
      { content },
    );
    return data;
  },
};
