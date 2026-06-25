import type {
  CompanySource,
  CompanyType,
  CustomerStatus,
  LeadStatus,
  Sector,
} from '../../../shared/types/enums';
import type { CategoryDto } from '../../category/model/category';

export interface CompanyDto {
  id: string;
  title: string;
  sector: Sector;
  phone: string;
  email: string;
  address: string;
  city: string | null;
  website: string | null;
  taxNumber: string | null;
  source: CompanySource | null;
  type: CompanyType;
  leadStatus: LeadStatus | null;
  customerStatus: CustomerStatus | null;
  activatedAtUtc: string | null;
  createdAtUtc: string;
  assignedSalesRepId: string | null;
  assignedSalesRepName: string | null;
  categories: CategoryDto[];
}

export interface ContactDto {
  id: string;
  companyId: string;
  name: string;
  email: string | null;
  phone: string | null;
}
