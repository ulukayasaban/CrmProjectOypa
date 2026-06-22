import {
  CUSTOMER_STATUS_LABELS,
  LEAD_STATUS_LABELS,
} from '../../../shared/constants/labels';
import type { CompanyDto } from '../../../entities/company/model/company';

interface StatusBadgeProps {
  company: CompanyDto;
}

export function StatusBadge({ company }: StatusBadgeProps) {
  if (company.type === 'Customer') {
    const label = company.customerStatus
      ? CUSTOMER_STATUS_LABELS[company.customerStatus]
      : 'Aktif';
    return <span className="badge badge-customer">{label}</span>;
  }
  const label = company.leadStatus
    ? LEAD_STATUS_LABELS[company.leadStatus]
    : 'Yeni';
  return <span className="badge badge-lead">{label}</span>;
}
