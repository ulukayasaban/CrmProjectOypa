import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import { dashboardApi } from '../api/dashboardApi';

export function useDashboard() {
  return useQuery({
    queryKey: queryKeys.dashboard,
    queryFn: dashboardApi.get,
  });
}
