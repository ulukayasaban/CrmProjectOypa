import type {
  CompanySource,
  CompanyType,
  CustomerStatus,
  FirmType,
  LeadStatus,
  Sector,
  ServiceSector,
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
  /** Kaynak altındaki serbest not (ör. "Belgin Öner referansı"). */
  sourceNote: string | null;
  type: CompanyType;
  leadStatus: LeadStatus | null;
  customerStatus: CustomerStatus | null;
  activatedAtUtc: string | null;
  createdAtUtc: string;
  assignedSalesRepId: string | null;
  assignedSalesRepName: string | null;
  /** OYPA'nın bu firmaya hangi sektörde hizmet verdiği. */
  serviceSector: ServiceSector | null;
  /** Firmanın OYAK Grubu içi mi dışı mı olduğu. */
  firmType: FirmType;
  /** Lead aşamasında iletişim kuran temsilci. */
  leadOwnerId: string | null;
  leadOwnerName: string | null;
  categories: CategoryDto[];
  /** Dönüşüm sırasında "Yeni Müşteri" olarak işaretlendi mi? */
  isNewCustomer: boolean;
  /** Son etkileşim zamanı (UTC ISO string). */
  lastInteractionAtUtc: string | null;
}

export interface ContactDto {
  id: string;
  companyId: string;
  name: string;
  email: string | null;
  phone: string | null;
}
