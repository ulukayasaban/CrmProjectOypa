import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import { salesRepApi, type SalesRepPayload } from '../api/salesRepApi';

export function useSalesReps() {
  return useQuery({
    queryKey: queryKeys.salesReps,
    queryFn: salesRepApi.getAll,
  });
}

export function useCreateSalesRep() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SalesRepPayload) => salesRepApi.create(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.salesReps });
    },
  });
}

export function useLinkEmployee() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, employeeId }: { id: string; employeeId: string | null }) =>
      salesRepApi.linkEmployee(id, employeeId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.salesReps });
    },
  });
}
