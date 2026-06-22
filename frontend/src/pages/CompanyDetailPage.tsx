import React, { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  useAssignSalesRep,
  useCompany,
  useCompanyContacts,
  useCompanyMeetings,
  useConvertCompany,
  useUpdateCustomerStatus,
  useUpdateLeadStatus,
} from '../features/companies/model/useCompanies';
import { useSalesReps } from '../features/salesreps/model/useSalesReps';
import { ContactFormModal } from '../features/companies/ui/ContactFormModal';
import { CompanyFormModal } from '../features/companies/ui/CompanyFormModal';
import { StatusBadge } from '../features/companies/ui/StatusBadge';
import { MeetingFormModal } from '../features/meetings/ui/MeetingFormModal';
import { useUpdateMeetingStatus } from '../features/meetings/model/useMeetings';
import { MeetingNotes } from '../features/meetings/ui/MeetingNotes';
import { mailDraftApi } from '../features/maildrafts/api/mailDraftApi';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { PlusIcon } from '../shared/components/icons';
import {
  LEAD_STATUS_OPTIONS,
  MEETING_METHOD_LABELS,
  MEETING_STATUS_LABELS,
  SECTOR_LABELS,
  SOURCE_LABELS,
} from '../shared/constants/labels';
import { queryKeys } from '../shared/api/queryKeys';
import type { LeadStatus } from '../shared/types/enums';
import { formatDate, formatTime } from '../shared/lib/format';
import { getErrorMessage } from '../shared/lib/errorMessage';
import { openInOutlook, saveEml } from '../shared/lib/outlook';
import { useAuth } from '../app/providers/useAuth';

interface MeetingMailActionsProps {
  meetingId: string;
}

/**
 * Fetches the mail draft for a meeting and renders Outlook/eml action buttons.
 * Buttons are disabled while loading and hidden when no draft exists (404).
 */
function MeetingMailActions({ meetingId }: MeetingMailActionsProps) {
  const [emlPending, setEmlPending] = useState(false);

  const { data: draft, isLoading } = useQuery({
    queryKey: queryKeys.mailDraftByMeeting(meetingId),
    queryFn: () => mailDraftApi.getByMeeting(meetingId),
    // A 404 means no draft exists for this meeting — treat as empty, not error.
    retry: false,
    throwOnError: false,
  });

  async function handleDownloadEml() {
    if (!draft) return;
    setEmlPending(true);
    try {
      const blob = await mailDraftApi.downloadEml(draft.id);
      saveEml(blob, `toplanti-${draft.id}.eml`);
    } finally {
      setEmlPending(false);
    }
  }

  if (isLoading) {
    return (
      <>
        <button type="button" className="btn btn-ghost btn-sm" disabled>
          Outlook'ta Ac
        </button>
        <button type="button" className="btn btn-ghost btn-sm" disabled>
          .eml
        </button>
      </>
    );
  }

  if (!draft) return null;

  return (
    <>
      <button
        type="button"
        className="btn btn-ghost btn-sm"
        onClick={() => openInOutlook(draft)}
      >
        Outlook'ta Ac
      </button>
      <button
        type="button"
        className="btn btn-ghost btn-sm"
        disabled={emlPending}
        onClick={() => void handleDownloadEml()}
      >
        {emlPending ? '...' : '.eml'}
      </button>
    </>
  );
}

export default function CompanyDetailPage() {
  const { id = '' } = useParams();
  const navigate = useNavigate();
  const [contactModal, setContactModal] = useState(false);
  const [meetingModal, setMeetingModal] = useState(false);
  const [opportunityModal, setOpportunityModal] = useState(false);
  const [expandedNoteId, setExpandedNoteId] = useState<string | null>(null);

  const { hasRole } = useAuth();
  const company = useCompany(id);
  const contacts = useCompanyContacts(id);
  const meetings = useCompanyMeetings(id);
  const convert = useConvertCompany(id);
  const updateLeadStatus = useUpdateLeadStatus(id);
  const updateCustomerStatus = useUpdateCustomerStatus(id);
  const updateStatus = useUpdateMeetingStatus();
  const assignSalesRep = useAssignSalesRep(id);
  const salesReps = useSalesReps();

  if (company.isLoading) return <Spinner />;
  if (company.isError || !company.data) {
    return <StateBlock message={getErrorMessage(company.error)} />;
  }

  const data = company.data;
  const isLead = data.type === 'Lead';
  const isCustomer = data.type === 'Customer';

  return (
    <>
      <div className="page-head">
        <button
          type="button"
          className="btn btn-ghost btn-sm"
          onClick={() => navigate(-1)}
        >
          ← Geri
        </button>
      </div>

      <div className="detail-grid">
        <div className="detail-col">
          <div className="glass card">
            <h3 style={{ marginBottom: 10 }}>{data.title}</h3>
            <p className="muted" style={{ fontSize: '0.9rem' }}>
              {SECTOR_LABELS[data.sector]}
            </p>
            <div style={{ marginTop: 15 }}>
              <StatusBadge company={data} />
            </div>
            {isLead && (
              <div className="form-group" style={{ marginTop: 15 }}>
                <label htmlFor="leadStatus">Lead Durumu</label>
                <select
                  id="leadStatus"
                  value={data.leadStatus ?? ''}
                  disabled={updateLeadStatus.isPending}
                  onChange={(event) =>
                    updateLeadStatus.mutate(event.target.value as LeadStatus)
                  }
                >
                  {LEAD_STATUS_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                {updateLeadStatus.isError && (
                  <span className="field-error">
                    {getErrorMessage(updateLeadStatus.error)}
                  </span>
                )}
              </div>
            )}
            <hr className="divider" />
            <div className="detail-info">
              <span>
                📍 <strong>Adres:</strong>
                <br />
                {data.address}
              </span>
              {data.city && (
                <span>
                  🏙️ <strong>Şehir:</strong>
                  <br />
                  {data.city}
                </span>
              )}
              <span>
                📧 <strong>E-posta:</strong>
                <br />
                {data.email}
              </span>
              <span>
                📞 <strong>Telefon:</strong>
                <br />
                {data.phone}
              </span>
              {data.website && (
                <span>
                  🌐 <strong>Web Sitesi:</strong>
                  <br />
                  <a href={data.website} target="_blank" rel="noopener noreferrer">
                    {data.website}
                  </a>
                </span>
              )}
              {data.taxNumber && (
                <span>
                  🧾 <strong>Vergi No:</strong>
                  <br />
                  {data.taxNumber}
                </span>
              )}
              {data.source && (
                <span>
                  📌 <strong>Kaynak:</strong>
                  <br />
                  {SOURCE_LABELS[data.source]}
                </span>
              )}
              <span>
                👤 <strong>Temsilci:</strong>
                <br />
                {data.assignedSalesRepName ?? 'Havuz (OYPA)'}
              </span>
              <span className="muted" style={{ fontSize: '0.75rem' }}>
                Oluşturulma: {formatDate(data.createdAtUtc)}
              </span>
            </div>
            {hasRole('Admin') && (
              <div className="form-group" style={{ marginTop: 15 }}>
                <label htmlFor="assignedSalesRep">Temsilci Ata</label>
                <select
                  id="assignedSalesRep"
                  value={data.assignedSalesRepId ?? ''}
                  disabled={assignSalesRep.isPending || salesReps.isLoading}
                  onChange={(event) => {
                    const value = event.target.value;
                    assignSalesRep.mutate(value === '' ? null : value);
                  }}
                >
                  <option value="">Havuza al (atama yok)</option>
                  {(salesReps.data ?? []).map((rep) => (
                    <option key={rep.id} value={rep.id}>
                      {rep.name}
                    </option>
                  ))}
                </select>
                {assignSalesRep.isError && (
                  <span className="field-error">
                    {getErrorMessage(assignSalesRep.error)}
                  </span>
                )}
              </div>
            )}
            <div className="detail-actions">
              {isLead && (
                <button
                  type="button"
                  className="btn btn-primary"
                  disabled={convert.isPending}
                  onClick={() => {
                    if (
                      window.confirm(
                        'Firmayı aktif müşteri portföyüne taşımak istiyor musunuz?',
                      )
                    ) {
                      convert.mutate();
                    }
                  }}
                >
                  {convert.isPending
                    ? 'Dönüştürülüyor...'
                    : 'Müşteriye Dönüştür'}
                </button>
              )}
              {isCustomer && (
                <button
                  type="button"
                  className="btn btn-ghost"
                  disabled={updateCustomerStatus.isPending}
                  onClick={() => {
                    const nextStatus =
                      data.customerStatus === 'Active' ? 'Passive' : 'Active';
                    updateCustomerStatus.mutate(nextStatus);
                  }}
                >
                  {updateCustomerStatus.isPending
                    ? 'Güncelleniyor...'
                    : data.customerStatus === 'Active'
                      ? 'Pasif Yap'
                      : 'Aktif Yap'}
                </button>
              )}
              {isCustomer && updateCustomerStatus.isError && (
                <span className="field-error" style={{ fontSize: '0.8rem' }}>
                  {getErrorMessage(updateCustomerStatus.error)}
                </span>
              )}
              <button
                type="button"
                className="btn btn-ghost"
                onClick={() => setOpportunityModal(true)}
              >
                <PlusIcon size={14} /> Yeni Fırsat Ekle
              </button>
              <button
                type="button"
                className="btn btn-ghost"
                onClick={() => setMeetingModal(true)}
              >
                📅 Randevu Planla
              </button>
            </div>
            {convert.isError && (
              <div className="form-error" style={{ marginTop: 12 }}>
                {getErrorMessage(convert.error)}
              </div>
            )}
          </div>
        </div>

        <div className="detail-col">
          <div className="glass card">
            <div className="card-head">
              <h4>İlgili Kişiler</h4>
              <button
                type="button"
                className="btn btn-ghost btn-sm"
                onClick={() => setContactModal(true)}
              >
                <PlusIcon size={14} /> Yeni Kişi
              </button>
            </div>
            {contacts.isLoading && <p className="muted">Yükleniyor...</p>}
            <div className="contacts-grid">
              {contacts.data && contacts.data.length === 0 && (
                <p className="muted" style={{ fontSize: '0.85rem' }}>
                  Henüz ilgili kişi eklenmemiş.
                </p>
              )}
              {(contacts.data ?? []).map((contact) => (
                <div key={contact.id} className="glass contact-card">
                  <div style={{ fontWeight: 700, marginBottom: 5 }}>
                    {contact.name}
                  </div>
                  <div className="muted">{contact.email ?? '-'}</div>
                  <div className="muted">{contact.phone ?? '-'}</div>
                </div>
              ))}
            </div>
          </div>

          <div className="glass card">
            <h4>Görüşme Kayıtları</h4>
            <div
              className="data-table-container"
              style={{ background: 'none', border: 'none', marginTop: 15 }}
            >
              <table className="data-table" style={{ fontSize: '0.85rem' }}>
                <thead>
                  <tr>
                    <th>Tarih</th>
                    <th>Temsilci</th>
                    <th>Yöntem</th>
                    <th>Durum</th>
                    <th>İşlem</th>
                    <th>Notlar</th>
                  </tr>
                </thead>
                <tbody>
                  {meetings.data && meetings.data.length === 0 && (
                    <tr>
                      <td colSpan={6} className="table-empty">
                        Görüşme kaydı yok.
                      </td>
                    </tr>
                  )}
                  {(meetings.data ?? []).map((meeting) => {
                    const isExpanded = expandedNoteId === meeting.id;
                    const noteCount = meeting.notes.length;
                    return (
                      <React.Fragment key={meeting.id}>
                        <tr>
                          <td>
                            {formatDate(meeting.date)}
                            <br />
                            {formatTime(meeting.time)}
                          </td>
                          <td>
                            <div>{meeting.salesRepName}</div>
                            {meeting.salesRepTitle && (
                              <div className="muted" style={{ fontSize: '0.75rem' }}>
                                {meeting.salesRepTitle}
                              </div>
                            )}
                          </td>
                          <td>{MEETING_METHOD_LABELS[meeting.method]}</td>
                          <td>
                            <span
                              className={`badge ${
                                meeting.status === 'Done'
                                  ? 'badge-customer'
                                  : meeting.status === 'Cancelled'
                                    ? 'badge-danger'
                                    : 'badge-lead'
                              }`}
                            >
                              {MEETING_STATUS_LABELS[meeting.status]}
                            </span>
                          </td>
                          <td>
                            <div className="row-actions">
                              {meeting.status === 'Planned' ? (
                                <button
                                  type="button"
                                  className="btn btn-ghost btn-sm"
                                  disabled={updateStatus.isPending}
                                  onClick={() =>
                                    updateStatus.mutate({
                                      id: meeting.id,
                                      status: 'Done',
                                      companyId: id,
                                    })
                                  }
                                >
                                  Yapıldı
                                </button>
                              ) : (
                                <span className="muted">-</span>
                              )}
                              <MeetingMailActions meetingId={meeting.id} />
                            </div>
                          </td>
                          <td>
                            <button
                              type="button"
                              className="btn btn-ghost btn-sm"
                              onClick={() =>
                                setExpandedNoteId(isExpanded ? null : meeting.id)
                              }
                            >
                              {noteCount > 0
                                ? `${noteCount} not ${isExpanded ? '▴' : '▾'}`
                                : `Not Ekle ${isExpanded ? '▴' : '▾'}`}
                            </button>
                          </td>
                        </tr>
                        {isExpanded && (
                          <tr>
                            <td
                              colSpan={6}
                              style={{
                                padding: '8px 16px',
                                background: 'rgba(0,0,0,0.15)',
                              }}
                            >
                              <MeetingNotes
                                meetingId={meeting.id}
                                companyId={id}
                                notes={meeting.notes}
                              />
                            </td>
                          </tr>
                        )}
                      </React.Fragment>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>

      {contactModal && (
        <ContactFormModal
          companyId={id}
          onClose={() => setContactModal(false)}
        />
      )}
      {meetingModal && (
        <MeetingFormModal
          company={data}
          onClose={() => setMeetingModal(false)}
        />
      )}
      {opportunityModal && (
        <CompanyFormModal
          prefill={{
            title: data.title,
            phone: data.phone,
            email: data.email,
            address: data.address,
          }}
          onClose={() => setOpportunityModal(false)}
        />
      )}
    </>
  );
}
