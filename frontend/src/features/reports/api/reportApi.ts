import { httpClient } from '../../../shared/api/httpClient';

export const reportApi = {
  /**
   * Downloads the meetings Excel report as a Blob.
   * The httpClient response interceptor bypasses envelope-unwrapping for
   * responseType:'blob', so the raw AxiosResponse is returned here.
   */
  async downloadMeetingReport(): Promise<Blob> {
    const response = await httpClient.get('/reports/meetings', {
      responseType: 'blob',
    });
    return response.data as Blob;
  },
};
