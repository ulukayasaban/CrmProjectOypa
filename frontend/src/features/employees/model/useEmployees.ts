import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import {
  employeeApi,
  type CreateEmployeePayload,
  type UpdateEmployeePayload,
  type AssignManagerPayload,
  type CreateAccountPayload,
  type AssignRolePayload,
} from '../api/employeeApi';

/** Queries managed employees (scope: current user's direct/indirect reports). */
export function useManagedEmployees() {
  return useQuery({
    queryKey: queryKeys.managedEmployees,
    queryFn: employeeApi.getManaged,
  });
}

export function useCreateEmployee() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateEmployeePayload) => employeeApi.create(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.managedEmployees });
      void queryClient.invalidateQueries({ queryKey: queryKeys.employees });
    },
  });
}

export function useUpdateEmployee() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateEmployeePayload }) =>
      employeeApi.update(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.managedEmployees });
      void queryClient.invalidateQueries({ queryKey: queryKeys.employees });
    },
  });
}

export function useRemoveEmployee() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => employeeApi.remove(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.managedEmployees });
      void queryClient.invalidateQueries({ queryKey: queryKeys.employees });
    },
  });
}

export function useAssignManager() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: AssignManagerPayload }) =>
      employeeApi.assignManager(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.managedEmployees });
      void queryClient.invalidateQueries({ queryKey: queryKeys.employees });
    },
  });
}

export function useCreateEmployeeAccount() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: CreateAccountPayload }) =>
      employeeApi.createAccount(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.managedEmployees });
      void queryClient.invalidateQueries({ queryKey: queryKeys.employees });
    },
  });
}

export function useAssignEmployeeRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: AssignRolePayload }) =>
      employeeApi.assignRole(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.managedEmployees });
      void queryClient.invalidateQueries({ queryKey: queryKeys.employees });
    },
  });
}

export function useResetEmployeePassword() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => employeeApi.resetPassword(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.managedEmployees });
    },
  });
}

export function useUnlinkEmployeeAccount() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => employeeApi.unlinkAccount(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.managedEmployees });
      void queryClient.invalidateQueries({ queryKey: queryKeys.employees });
    },
  });
}
