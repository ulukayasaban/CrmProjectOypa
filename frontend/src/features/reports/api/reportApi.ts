import { httpClient } from '../../../shared/api/httpClient';

export const reportApi = {
  /**
   * Görüşme Excel raporunu Blob olarak indirir.
   * httpClient interceptor'ı responseType:'blob' için envelope sarmasını atlar;
   * ham AxiosResponse döner.
   */
  async downloadMeetingReport(): Promise<Blob> {
    const response = await httpClient.get('/reports/meetings', {
      responseType: 'blob',
    });
    return response.data as Blob;
  },

  /**
   * İhale Excel raporunu Blob olarak indirir.
   * Görüşme raporu ile aynı blob bypass mekanizması kullanılır.
   */
  async downloadTenderReport(): Promise<Blob> {
    const response = await httpClient.get('/reports/tenders', {
      responseType: 'blob',
    });
    return response.data as Blob;
  },
};
