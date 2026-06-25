import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import { categoryApi, type CategoryPayload } from '../api/categoryApi';
import { useToast } from '../../../shared/components/toast/ToastProvider';

export function useCategories() {
  return useQuery({
    queryKey: queryKeys.categories,
    queryFn: categoryApi.getAll,
  });
}

export function useCreateCategory() {
  const queryClient = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: (payload: CategoryPayload) => categoryApi.create(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.categories });
      toast.success('Kategori oluşturuldu.');
    },
  });
}

export function useUpdateCategory() {
  const queryClient = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: CategoryPayload }) =>
      categoryApi.update(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.categories });
      toast.success('Kategori güncellendi.');
    },
  });
}

export function useDeleteCategory() {
  const queryClient = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: (id: string) => categoryApi.remove(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.categories });
      toast.success('Kategori silindi.');
    },
  });
}

export function useSetCompanyCategories(companyId: string) {
  const queryClient = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: (categoryIds: string[]) =>
      categoryApi.setCompanyCategories(companyId, categoryIds),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.company(companyId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.leads });
      void queryClient.invalidateQueries({ queryKey: queryKeys.customers });
      toast.success('Kategoriler güncellendi.');
    },
  });
}
