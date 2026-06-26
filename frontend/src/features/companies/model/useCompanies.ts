import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import {
  companyApi,
  type CompanyPayload,
  type ContactPayload,
  type ConvertPayload,
  type CustomersPagedParams,
  type LeadsPagedParams,
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

/**
 * Sunucu taraflı sayfalı lead sorgusu.
 * Takvim/Sidebar gibi tam-liste tüketicilerini etkilemez.
 */
export function useLeadsPaged(params: LeadsPagedParams) {
  return useQuery({
    queryKey: queryKeys.leadsPaged(params),
    queryFn: () => companyApi.getLeadsPaged(params),
    placeholderData: keepPreviousData,
  });
}

/**
 * Sunucu taraflı sayfalı müşteri sorgusu.
 * Tam-liste useCustomers hook'u bozulmaz.
 * status opsiyoneldir; verilmezse tüm müşteriler (aktif+pasif) döner.
 */
export function useCustomersPaged(params: Omit<CustomersPagedParams, 'status'> & { status?: CustomerStatus }) {
  return useQuery({
    queryKey: queryKeys.customersPaged(params),
    queryFn: () => companyApi.getCustomersPaged(params),
    placeholderData: keepPreviousData,
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
    mutationFn: (payload?: ConvertPayload) => companyApi.convert(id, payload),
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

/** İlgili kişiyi günceller; başarı durumunda ilgili şirketin contact listesi yenilenir. */
export function useUpdateContact(companyId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      contactId,
      payload,
    }: {
      contactId: string;
      payload: ContactPayload;
    }) => companyApi.updateContact(contactId, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: queryKeys.companyContacts(companyId),
      });
    },
  });
}

/** İlgili kişiyi siler; başarı durumunda ilgili şirketin contact listesi yenilenir. */
export function useDeleteContact(companyId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (contactId: string) => companyApi.deleteContact(contactId),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: queryKeys.companyContacts(companyId),
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
