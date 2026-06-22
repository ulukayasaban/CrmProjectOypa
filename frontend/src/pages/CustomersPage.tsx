import { useNavigate, useParams, Navigate } from 'react-router-dom';
import { useCustomers } from '../features/companies/model/useCompanies';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { SECTOR_LABELS } from '../shared/constants/labels';
import { formatDate } from '../shared/lib/format';
import { getErrorMessage } from '../shared/lib/errorMessage';
import type { CustomerStatus } from '../shared/types/enums';

const SEGMENT_MAP: Record<string, CustomerStatus> = {
  aktif: 'Active',
  pasif: 'Passive',
};

const SEGMENT_TITLE: Record<string, string> = {
  aktif: 'Aktif Müşteri Portföyü',
  pasif: 'Pasif Müşteri Portföyü',
};

export default function CustomersPage() {
  const navigate = useNavigate();
  const { segment = '' } = useParams<{ segment: string }>();

  const status = SEGMENT_MAP[segment];

  // Invalid segment → redirect to aktif
  if (!status) {
    return <Navigate to="/customers/aktif" replace />;
  }

  return <CustomersContent status={status} title={SEGMENT_TITLE[segment]} navigate={navigate} />;
}

interface CustomersContentProps {
  status: CustomerStatus;
  title: string;
  navigate: ReturnType<typeof useNavigate>;
}

function CustomersContent({ status, title, navigate }: CustomersContentProps) {
  const { data, isLoading, isError, error } = useCustomers(status);

  if (isLoading) return <Spinner />;
  if (isError || !data) return <StateBlock message={getErrorMessage(error)} />;

  return (
    <>
      <div className="page-head">
        <h3>{title}</h3>
      </div>
      <div className="data-table-container glass">
        <table className="data-table">
          <thead>
            <tr>
              <th>Firma</th>
              <th>Sektör</th>
              <th>Aktif Geçiş Tarihi</th>
              <th>İletişim</th>
              <th>Temsilci</th>
              <th>İşlem</th>
            </tr>
          </thead>
          <tbody>
            {data.length === 0 && (
              <tr>
                <td colSpan={6} className="table-empty">
                  {status === 'Active'
                    ? 'Henüz aktif müşteriniz bulunmuyor.'
                    : 'Henüz pasif müşteriniz bulunmuyor.'}
                </td>
              </tr>
            )}
            {data.map((company) => (
              <tr
                key={company.id}
                className="clickable"
                onClick={() => navigate(`/companies/${company.id}`)}
              >
                <td>
                  <strong>{company.title}</strong>
                </td>
                <td>
                  <span className="badge badge-opportunity">
                    {SECTOR_LABELS[company.sector]}
                  </span>
                </td>
                <td style={{ fontSize: '0.85rem' }}>
                  {formatDate(company.activatedAtUtc)}
                </td>
                <td style={{ fontSize: '0.85rem' }}>{company.email}</td>
                <td style={{ fontSize: '0.85rem' }}>
                  {company.assignedSalesRepName ?? 'Havuz'}
                </td>
                <td>
                  <button
                    type="button"
                    className="btn btn-ghost btn-sm"
                    onClick={(event) => {
                      event.stopPropagation();
                      navigate(`/companies/${company.id}`);
                    }}
                  >
                    Dosyayı Aç
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
