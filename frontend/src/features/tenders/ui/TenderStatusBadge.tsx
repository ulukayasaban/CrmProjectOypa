import { TENDER_STATUS_LABELS } from '../../../shared/constants/labels';
import type { TenderStatus } from '../../../entities/tender/model/tender';

interface TenderStatusBadgeProps {
  status: TenderStatus;
}

/**
 * İhale durumunu renk kodlu rozet olarak gösterir.
 * İhaleler sayfası ve takvim gibi tüm görünümlerde tek kaynaktan kullanılır
 * (renk/etiket tutarlılığı için).
 */
export function TenderStatusBadge({ status }: TenderStatusBadgeProps) {
  const className =
    status === 'Kazanildi'
      ? 'badge badge-customer'
      : status === 'Kaybedildi' || status === 'Iptal'
        ? 'badge badge-danger'
        : status === 'TeklifVerildi'
          ? 'badge badge-lead'
          : 'badge badge-opportunity';

  return <span className={className}>{TENDER_STATUS_LABELS[status]}</span>;
}
