import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import { tenderApi, type TenderListParams, type TenderPayload } from '../api/tenderApi';
import type { TenderStatus } from '../../../entities/tender/model/tender';

export function useTenders(params?: TenderListParams) {
  return useQuery({
    queryKey: params ? [...queryKeys.tenders, params] : queryKeys.tenders,
    queryFn: () => tenderApi.getAll(params),
  });
}

export function useTender(id: string) {
  return useQuery({
    queryKey: queryKeys.tender(id),
    queryFn: () => tenderApi.getById(id),
    enabled: id.length > 0,
  });
}

export function useCreateTender() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: TenderPayload) => tenderApi.create(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.tenders });
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
    },
  });
}

export function useUpdateTender() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: TenderPayload }) =>
      tenderApi.update(id, payload),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.tenders });
      void queryClient.invalidateQueries({
        queryKey: queryKeys.tender(variables.id),
      });
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
    },
  });
}

export function useChangeTenderStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: TenderStatus }) =>
      tenderApi.changeStatus(id, status),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.tenders });
      void queryClient.invalidateQueries({
        queryKey: queryKeys.tender(variables.id),
      });
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
    },
  });
}

export function useDeleteTender() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => tenderApi.remove(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.tenders });
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
    },
  });
}
