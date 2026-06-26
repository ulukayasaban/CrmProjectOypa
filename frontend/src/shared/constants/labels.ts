import type {
  CompanySource,
  CompanyType,
  CustomerStatus,
  FirmType,
  LeadStatus,
  MeetingMethod,
  MeetingStatus,
  Sector,
  ServiceSector,
  UserRole,
} from '../types/enums';
import type { TenderStatus } from '../../entities/tender/model/tender';

export const SECTOR_LABELS: Record<Sector, string> = {
  Tourism: 'Turizm',
  Retail: 'Perakende',
  FacilityManagement: 'Tesis Yönetimi',
  Energy: 'Enerji',
  Other: 'Diğer',
};

export const COMPANY_TYPE_LABELS: Record<CompanyType, string> = {
  Lead: 'Potansiyel Müşteri',
  Customer: 'Aktif Müşteri',
};

export const LEAD_STATUS_LABELS: Record<LeadStatus, string> = {
  New: 'Yeni',
  Contacted: 'İletişim Kuruldu',
  Qualified: 'Nitelikli',
  Lost: 'Kaybedildi',
};

export const CUSTOMER_STATUS_LABELS: Record<CustomerStatus, string> = {
  Active: 'Aktif',
  Passive: 'Pasif',
};

export const SOURCE_LABELS: Record<CompanySource, string> = {
  Referral: 'Referans',
  Website: 'Web Sitesi',
  Fair: 'Fuar',
  ColdCall: 'Soğuk Arama',
  Other: 'Diğer',
};

export const MEETING_METHOD_LABELS: Record<MeetingMethod, string> = {
  Visit: 'Yüz Yüze Ziyaret',
  Phone: 'Telefon Görüşmesi',
  Email: 'E-mail / Teklif',
};

export const MEETING_STATUS_LABELS: Record<MeetingStatus, string> = {
  Planned: 'Planlandı',
  Done: 'Yapıldı',
  Cancelled: 'İptal Edildi',
};

export const TENDER_STATUS_LABELS: Record<TenderStatus, string> = {
  Hazirlik: 'Hazırlık',
  TeklifVerildi: 'Teklif Verildi',
  Kazanildi: 'Kazanıldı',
  Kaybedildi: 'Kaybedildi',
  Iptal: 'İptal',
};

export const SECTOR_OPTIONS: ReadonlyArray<{ value: Sector; label: string }> = (
  Object.keys(SECTOR_LABELS) as Sector[]
).map((value) => ({ value, label: SECTOR_LABELS[value] }));

export const LEAD_STATUS_OPTIONS: ReadonlyArray<{
  value: LeadStatus;
  label: string;
}> = (Object.keys(LEAD_STATUS_LABELS) as LeadStatus[]).map((value) => ({
  value,
  label: LEAD_STATUS_LABELS[value],
}));

export const MEETING_METHOD_OPTIONS: ReadonlyArray<{
  value: MeetingMethod;
  label: string;
}> = (Object.keys(MEETING_METHOD_LABELS) as MeetingMethod[]).map((value) => ({
  value,
  label: MEETING_METHOD_LABELS[value],
}));

export const USER_ROLE_LABELS: Record<UserRole, string> = {
  Admin: 'Yönetici',
  Sales: 'Satış Temsilcisi',
};

export const USER_ROLE_OPTIONS: ReadonlyArray<{
  value: UserRole;
  label: string;
}> = (Object.keys(USER_ROLE_LABELS) as UserRole[]).map((value) => ({
  value,
  label: USER_ROLE_LABELS[value],
}));

export const SOURCE_OPTIONS: ReadonlyArray<{
  value: CompanySource;
  label: string;
}> = (Object.keys(SOURCE_LABELS) as CompanySource[]).map((value) => ({
  value,
  label: SOURCE_LABELS[value],
}));

export const CUSTOMER_STATUS_OPTIONS: ReadonlyArray<{
  value: CustomerStatus;
  label: string;
}> = (Object.keys(CUSTOMER_STATUS_LABELS) as CustomerStatus[]).map((value) => ({
  value,
  label: CUSTOMER_STATUS_LABELS[value],
}));

export const TENDER_STATUS_OPTIONS: ReadonlyArray<{
  value: TenderStatus;
  label: string;
}> = (Object.keys(TENDER_STATUS_LABELS) as TenderStatus[]).map((value) => ({
  value,
  label: TENDER_STATUS_LABELS[value],
}));

export const SERVICE_SECTOR_LABELS: Record<ServiceSector, string> = {
  TesisYonetimi: 'Tesis Yönetimi',
  Turizm: 'Turizm',
  Perakende: 'Perakende',
};

export const SERVICE_SECTOR_OPTIONS: ReadonlyArray<{
  value: ServiceSector;
  label: string;
}> = (Object.keys(SERVICE_SECTOR_LABELS) as ServiceSector[]).map((value) => ({
  value,
  label: SERVICE_SECTOR_LABELS[value],
}));

export const FIRM_TYPE_LABELS: Record<FirmType, string> = {
  IcFirma: 'İç Firma (OYAK Grubu)',
  DisFirma: 'Dış Firma',
};

export const FIRM_TYPE_OPTIONS: ReadonlyArray<{
  value: FirmType;
  label: string;
}> = (Object.keys(FIRM_TYPE_LABELS) as FirmType[]).map((value) => ({
  value,
  label: FIRM_TYPE_LABELS[value],
}));
