/** Sunucudan dönen sayfalı sonuç zarfı. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** Tüm paged sorgularda kullanılan ortak query parametreleri. */
export interface PagedParams {
  page: number;
  pageSize: number;
  search?: string;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
}
