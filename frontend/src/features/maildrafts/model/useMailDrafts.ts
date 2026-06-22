import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import { mailDraftApi } from '../api/mailDraftApi';

export function useMailDrafts() {
  return useQuery({
    queryKey: queryKeys.mailDrafts,
    queryFn: mailDraftApi.getAll,
  });
}

export function useSendMailDraft() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => mailDraftApi.send(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.mailDrafts });
      void queryClient.invalidateQueries({ queryKey: queryKeys.notifications });
    },
  });
}
