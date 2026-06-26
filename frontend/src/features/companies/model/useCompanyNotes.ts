import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import { companyNoteApi } from '../api/companyNoteApi';
import { useToast } from '../../../shared/components/toast/ToastProvider';
import { getErrorMessage } from '../../../shared/lib/errorMessage';

export function useCompanyNotes(companyId: string) {
  return useQuery({
    queryKey: queryKeys.companyNotes(companyId),
    queryFn: () => companyNoteApi.list(companyId),
    enabled: companyId.length > 0,
  });
}

export function useAddCompanyNote(companyId: string) {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (content: string) => companyNoteApi.create(companyId, content),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: queryKeys.companyNotes(companyId),
      });
      toast.success('Not kaydedildi.');
    },
    onError: (err) => {
      toast.error(getErrorMessage(err));
    },
  });
}
