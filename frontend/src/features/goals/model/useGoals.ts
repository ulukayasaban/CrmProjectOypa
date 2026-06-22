import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import {
  goalApi,
  type CreateGoalPayload,
  type UpdateGoalPayload,
} from '../api/goalApi';

export function useGoals() {
  return useQuery({
    queryKey: queryKeys.goals,
    queryFn: goalApi.getScoped,
  });
}

export function useGoalWeeks(id: string) {
  return useQuery({
    queryKey: [...queryKeys.goals, id, 'weeks'] as const,
    queryFn: () => goalApi.getWeeks(id),
    enabled: Boolean(id),
  });
}

export function useCreateGoal() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateGoalPayload) => goalApi.create(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.goals });
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
    },
  });
}

export function useUpdateGoal() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateGoalPayload }) =>
      goalApi.update(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.goals });
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
    },
  });
}

export function useDeleteGoal() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => goalApi.remove(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.goals });
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
    },
  });
}
