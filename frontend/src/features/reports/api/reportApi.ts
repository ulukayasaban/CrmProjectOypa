import { httpClient } from '../../../shared/api/httpClient';

/** Rapor tarih aralığı filtresi (her iki uç da opsiyonel; yyyy-MM-dd). */
export interface ReportDateRange {
  from?: string;
  to?: string;
}

/** from/to dolu olanları query parametresine çevirir. */
function rangeParams(range?: ReportDateRange): Record<string, string> {
  const params: Record<string, string> = {};
  if (range?.from) params.from = range.from;
  if (range?.to) params.to = range.to;
  return params;
}

/**
 * Verilen uçtan Excel raporunu Blob olarak indirir.
 * httpClient interceptor'ı responseType:'blob' için envelope sarmasını atlar.
 */
async function downloadReport(
  path: string,
  range?: ReportDateRange,
): Promise<Blob> {
  const response = await httpClient.get(path, {
    responseType: 'blob',
    params: rangeParams(range),
  });
  return response.data as Blob;
}

export const reportApi = {
  /** Görüşme Excel raporu (opsiyonel tarih aralığı — görüşme tarihine göre). */
  downloadMeetingReport: (range?: ReportDateRange) =>
    downloadReport('/reports/meetings', range),

  /** İhale Excel raporu (opsiyonel tarih aralığı — ihale tarihine göre). */
  downloadTenderReport: (range?: ReportDateRange) =>
    downloadReport('/reports/tenders', range),

  /** Hedef Excel raporu (aktif hedefler + ilerleme). */
  downloadGoalReport: () => downloadReport('/reports/goals'),

  /** Müşteri Excel raporu (opsiyonel tarih aralığı — oluşturulma tarihine göre). */
  downloadCustomerReport: (range?: ReportDateRange) =>
    downloadReport('/reports/customers', range),
};
