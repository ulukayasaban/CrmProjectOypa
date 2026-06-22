import type { Sector } from '../../../shared/types/enums';

export type TenderStatus =
  | 'Hazirlik'
  | 'TeklifVerildi'
  | 'Kazanildi'
  | 'Kaybedildi'
  | 'Iptal';

export interface TenderDto {
  id: string;
  companyId: string;
  companyTitle: string;
  title: string;
  tenderNumber: string | null;
  sector: Sector;
  tenderDate: string;
  status: TenderStatus;
  personnelCount: number | null;
  estimatedValue: number | null;
  volume: number | null;
  quantity: number | null;
  description: string | null;
  assignedSalesRepId: string | null;
  assignedSalesRepName: string | null;
  createdAtUtc: string;
}
