import { useQuery } from '@tanstack/react-query';
import { employeeApi } from '../api/employeeApi';

const QUERY_KEY = ['employees'] as const;

export function useEmployees() {
  return useQuery({
    queryKey: QUERY_KEY,
    queryFn: employeeApi.getAll,
  });
}
