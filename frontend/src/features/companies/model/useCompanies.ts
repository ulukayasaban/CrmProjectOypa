import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import {
  companyApi,
  type CompanyPayload,
  type ContactPayload,
} from '../api/companyApi';
import type { CustomerStatus, LeadStatus } from '../../../shared/types/enums';

export function useLeads(status?: LeadStatus) {
  return useQuery({
    queryKey: status ? [...queryKeys.leads, status] : queryKeys.leads,
    queryFn: () => companyApi.getLeads(status),
  });
}

export function useCustomers(status?: CustomerStatus) {
  return useQuery({
    queryKey: status ? [...queryKeys.customers, status] : queryKeys.customers,
    queryFn: () => companyApi.getCustomers(status),
  });
}

export function useCompany(id: string) {
  return useQuery({
    queryKey: queryKeys.company(id),
    queryFn: () => companyApi.getById(id),
    enabled: id.length > 0,
  });
}

export function useCompanyContacts(id: string) {
  return useQuery({
    queryKey: queryKeys.companyContacts(id),
    queryFn: () => companyApi.getContacts(id),
    enabled: id.length > 0,
  });
}

export function useCompanyMeetings(id: string) {
  return useQuery({
    queryKey: queryKeys.companyMeetings(id),
    queryFn: () => companyApi.getMeetings(id),
    enabled: id.length > 0,
  });
}

export function useCreateCompany() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CompanyPayload) => companyApi.create(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.leads });
    },
  });
}

export function useConvertCompany(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => companyApi.convert(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.company(id) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.leads });
      void queryClient.invalidateQueries({ queryKey: queryKeys.customers });
      // Lead→Müşteri dönüşümü "Aktif Leadler"/"Toplam Müşteri" sayaçlarını değiştirir.
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
    },
  });
}

export function useUpdateLeadStatus(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (status: LeadStatus) =>
      companyApi.updateLeadStatus(id, status),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.company(id) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.leads });
    },
  });
}

export function useUpdateCustomerStatus(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (status: CustomerStatus) =>
      companyApi.updateCustomerStatus(id, status),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.company(id) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.customers });
    },
  });
}

export function useCreateContact(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: ContactPayload) =>
      companyApi.createContact(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: queryKeys.companyContacts(id),
      });
    },
  });
}

export function useAssignSalesRep(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (salesRepId: string | null) =>
      companyApi.assignSalesRep(id, salesRepId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.leads });
      void queryClient.invalidateQueries({ queryKey: queryKeys.customers });
      void queryClient.invalidateQueries({ queryKey: queryKeys.company(id) });
    },
  });
}
