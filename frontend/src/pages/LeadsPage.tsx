import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useLeads } from '../features/companies/model/useCompanies';
import { CompanyFormModal } from '../features/companies/ui/CompanyFormModal';
import { StatusBadge } from '../features/companies/ui/StatusBadge';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { PlusIcon } from '../shared/components/icons';
import { LEAD_STATUS_LABELS, SECTOR_LABELS } from '../shared/constants/labels';
import { getErrorMessage } from '../shared/lib/errorMessage';
import type { CompanyDto } from '../entities/company/model/company';
import type { LeadStatus } from '../shared/types/enums';

type LeadStatusFilter = LeadStatus | undefined;

const LEAD_TABS: ReadonlyArray<{ value: LeadStatusFilter; label: string }> = [
  { value: undefined, label: 'Tümü' },
  { value: 'New', label: LEAD_STATUS_LABELS.New },
  { value: 'Contacted', label: LEAD_STATUS_LABELS.Contacted },
  { value: 'Qualified', label: LEAD_STATUS_LABELS.Qualified },
  { value: 'Lost', label: LEAD_STATUS_LABELS.Lost },
];

const POOL_KEY = '\x00pool'; // sentinel key for null reps — sorts before alphabetic

function groupByRep(
  companies: CompanyDto[],
): Array<{ repName: string; items: CompanyDto[] }> {
  const map = new Map<string, CompanyDto[]>();

  for (const company of companies) {
    const key = company.assignedSalesRepName ?? POOL_KEY;
    const bucket = map.get(key) ?? [];
    bucket.push(company);
    map.set(key, bucket);
  }

  const groups: Array<{ repName: string; items: CompanyDto[] }> = [];

  // Pool group always first
  if (map.has(POOL_KEY)) {
    groups.push({ repName: 'Havuz (OYPA)', items: map.get(POOL_KEY)! });
    map.delete(POOL_KEY);
  }

  // Remaining groups sorted alphabetically by rep name
  const sorted = Array.from(map.entries()).sort(([a], [b]) =>
    a.localeCompare(b, 'tr'),
  );
  for (const [repName, items] of sorted) {
    groups.push({ repName, items });
  }

  return groups;
}

export default function LeadsPage() {
  const navigate = useNavigate();
  const [modalOpen, setModalOpen] = useState(false);
  const [activeStatus, setActiveStatus] = useState<LeadStatusFilter>(undefined);
  const { data, isLoading, isError, error } = useLeads(activeStatus);

  const groups = data ? groupByRep(data) : [];

  return (
    <>
      <div className="page-head">
        <h3>Lead & Fırsat Listesi</h3>
        <button
          type="button"
          className="btn btn-primary"
          onClick={() => setModalOpen(true)}
        >
          <PlusIcon /> Yeni Firma / Fırsat
        </button>
      </div>

      <div className="tab-bar">
        {LEAD_TABS.map((tab) => (
          <button
            key={tab.value ?? 'all'}
            type="button"
            className={`tab-btn${activeStatus === tab.value ? ' tab-btn--active' : ''}`}
            onClick={() => setActiveStatus(tab.value)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {isLoading && <Spinner />}
      {isError && <StateBlock message={getErrorMessage(error)} />}

      {data && (
        <>
          {groups.length === 0 && (
            <div className="data-table-container glass">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Firma Ünvanı</th>
                    <th>Sektör</th>
                    <th>Adres</th>
                    <th>Durum</th>
                    <th>İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td colSpan={5} className="table-empty">
                      Sistemde aktif lead bulunmuyor.
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          )}
          {groups.map((group) => (
            <div key={group.repName} style={{ marginBottom: 24 }}>
              <h4
                style={{
                  margin: '0 0 8px 0',
                  fontSize: '0.95rem',
                  fontWeight: 600,
                  opacity: 0.75,
                  letterSpacing: '0.02em',
                }}
              >
                {group.repName}
              </h4>
              <div className="data-table-container glass">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Firma Ünvanı</th>
                      <th>Sektör</th>
                      <th>Adres</th>
                      <th>Durum</th>
                      <th>İşlem</th>
                    </tr>
                  </thead>
                  <tbody>
                    {group.items.map((company) => (
                      <tr
                        key={company.id}
                        className="clickable"
                        onClick={() => navigate(`/companies/${company.id}`)}
                      >
                        <td>
                          <strong>{company.title}</strong>
                        </td>
                        <td>
                          <span className="badge badge-neutral">
                            {SECTOR_LABELS[company.sector]}
                          </span>
                        </td>
                        <td className="muted" style={{ fontSize: '0.85rem' }}>
                          {company.address}
                        </td>
                        <td>
                          <StatusBadge company={company} />
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
                            Detay / İşlem
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ))}
        </>
      )}

      {modalOpen && (
        <CompanyFormModal
          onClose={() => setModalOpen(false)}
          onCreated={(company) => navigate(`/companies/${company.id}`)}
        />
      )}
    </>
  );
}
