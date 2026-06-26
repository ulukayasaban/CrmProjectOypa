import React, { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  useAssignSalesRep,
  useCompany,
  useCompanyContacts,
  useCompanyMeetings,
  useDeleteContact,
  useUpdateCustomerStatus,
  useUpdateLeadStatus,
} from '../features/companies/model/useCompanies';
import { useSalesReps } from '../features/salesreps/model/useSalesReps';
import { ContactFormModal } from '../features/companies/ui/ContactFormModal';
import { CompanyFormModal } from '../features/companies/ui/CompanyFormModal';
import { ConvertToCustomerModal } from '../features/companies/ui/ConvertToCustomerModal';
import { StatusBadge } from '../features/companies/ui/StatusBadge';
import { MeetingFormModal } from '../features/meetings/ui/MeetingFormModal';
import { useUpdateMeetingStatus } from '../features/meetings/model/useMeetings';
import { MeetingNotes } from '../features/meetings/ui/MeetingNotes';
import { mailDraftApi } from '../features/maildrafts/api/mailDraftApi';
import { CategoryBadges } from '../features/categories/ui/CategoryBadges';
import { CategoryMultiSelect } from '../features/categories/ui/CategoryMultiSelect';
import { useCategories, useSetCompanyCategories } from '../features/categories/model/useCategories';
import { Modal } from '../shared/components/Modal';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { PlusIcon } from '../shared/components/icons';
import { useToast } from '../shared/components/toast/ToastProvider';
import { useConfirm } from '../shared/hooks/useConfirm';
import {
  FIRM_TYPE_LABELS,
  LEAD_STATUS_OPTIONS,
  MEETING_METHOD_LABELS,
  MEETING_STATUS_LABELS,
  SECTOR_LABELS,
  SERVICE_SECTOR_LABELS,
  SOURCE_LABELS,
} from '../shared/constants/labels';
import { queryKeys } from '../shared/api/queryKeys';
import { toLeadStatus } from '../shared/lib/narrowing';
import { formatDate, formatTime } from '../shared/lib/format';
import { getErrorMessage } from '../shared/lib/errorMessage';
import { openInOutlook, saveEml } from '../shared/lib/outlook';
import { useAuth } from '../app/providers/useAuth';
import { CompanyNotes } from '../features/companies/ui/CompanyNotes';
import type { ContactDto } from '../entities/company/model/company';

interface MeetingMailActionsProps {
  meetingId: string;
}

/**
 * Görüşmeye ait mail taslağını çeker ve Outlook/.eml aksiyon butonlarını render eder.
 * Taslak yoksa (404) null döner.
 */
function MeetingMailActions({ meetingId }: MeetingMailActionsProps) {
  const [emlPending, setEmlPending] = useState(false);

  const { data: draft, isLoading } = useQuery({
    queryKey: queryKeys.mailDraftByMeeting(meetingId),
    queryFn: () => mailDraftApi.getByMeeting(meetingId),
    // 404 → bu görüşme için taslak yok; hata olarak ele alma.
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
  const toast = useToast();
  const { confirm, ConfirmEl } = useConfirm();

  const [contactModal, setContactModal] = useState(false);
  // Düzenleme modunda açmak için seçili contact; undefined → yeni kişi ekle
  const [editingContact, setEditingContact] = useState<ContactDto | undefined>(undefined);
  const [meetingModal, setMeetingModal] = useState(false);
  const [opportunityModal, setOpportunityModal] = useState(false);
  const [convertModal, setConvertModal] = useState(false);
  const [expandedNoteId, setExpandedNoteId] = useState<string | null>(null);
  const [categoryModal, setCategoryModal] = useState(false);

  const { hasRole } = useAuth();
  const company = useCompany(id);
  const contacts = useCompanyContacts(id);
  const meetings = useCompanyMeetings(id);
  const updateLeadStatus = useUpdateLeadStatus(id);
  const updateCustomerStatus = useUpdateCustomerStatus(id);
  const updateStatus = useUpdateMeetingStatus();
  const assignSalesRep = useAssignSalesRep(id);
  const salesReps = useSalesReps();
  const deleteContact = useDeleteContact(id);

  if (company.isLoading) return <Spinner />;
  if (company.isError || !company.data) {
    return <StateBlock message={getErrorMessage(company.error)} />;
  }

  const data = company.data;
  const isLead = data.type === 'Lead';
  const isCustomer = data.type === 'Customer';

  /** İlgili kişiyi onay alarak siler. */
  async function handleDeleteContact(contact: ContactDto) {
    const confirmed = await confirm({
      title: 'İlgili Kişiyi Sil',
      message: `"${contact.name}" kişisini silmek istiyor musunuz? Bu işlem geri alınamaz.`,
      confirmLabel: 'Sil',
    });
    if (!confirmed) return;

    try {
      await deleteContact.mutateAsync(contact.id);
      toast.success('İlgili kişi silindi.');
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  }

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
            <h3 style={{ marginBottom: 10 }}>
              {data.title}
              {data.isNewCustomer && (
                <span
                  className="badge badge-lead"
                  style={{ marginLeft: 10, fontSize: '0.7rem', verticalAlign: 'middle' }}
                  title="Yeni müşteri"
                >
                  YENİ
                </span>
              )}
            </h3>
            <p className="muted" style={{ fontSize: '0.9rem' }}>
              {SECTOR_LABELS[data.sector]}
            </p>
            <div style={{ marginTop: 8, display: 'flex', gap: 8, flexWrap: 'wrap' }}>
              <StatusBadge company={data} />
              <span
                className={`badge ${data.firmType === 'IcFirma' ? 'badge-lead' : 'badge-neutral'}`}
                title="Firma Tipi"
              >
                {FIRM_TYPE_LABELS[data.firmType]}
              </span>
            </div>
            {isLead && (
              <div className="form-group" style={{ marginTop: 15 }}>
                <label htmlFor="leadStatus">Lead Durumu</label>
                <select
                  id="leadStatus"
                  aria-label="Lead durumu seç"
                  value={data.leadStatus ?? ''}
                  disabled={updateLeadStatus.isPending}
                  onChange={(event) => {
                    // LEAD_STATUS_OPTIONS'tan gelen değer; narrowing ile runtime güvenli daraltma.
                    const status = toLeadStatus(event.target.value);
                    if (status !== undefined) {
                      updateLeadStatus.mutate(status, {
                        onSuccess: () => toast.success('Lead durumu güncellendi.'),
                        onError: (err) => toast.error(getErrorMessage(err)),
                      });
                    }
                  }}
                >
                  {LEAD_STATUS_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
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
              {data.sourceNote && (
                <span>
                  📝 <strong>Kaynak Notu:</strong>
                  <br />
                  {data.sourceNote}
                </span>
              )}
              {data.serviceSector && (
                <span>
                  🏷️ <strong>Hizmet Verilecek Sektör:</strong>
                  <br />
                  {SERVICE_SECTOR_LABELS[data.serviceSector]}
                </span>
              )}
              {data.leadOwnerName && (
                <span>
                  🤝 <strong>Atanan Lead:</strong>
                  <br />
                  {data.leadOwnerName}
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

            {/* Kategoriler */}
            <div style={{ marginTop: 15 }}>
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 8,
                  marginBottom: 6,
                }}
              >
                <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                  <strong>Kategoriler:</strong>
                </span>
                <button
                  type="button"
                  className="btn btn-ghost btn-sm"
                  onClick={() => setCategoryModal(true)}
                >
                  Düzenle
                </button>
              </div>
              <CategoryBadges categories={data.categories} />
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
                    assignSalesRep.mutate(value === '' ? null : value, {
                      onSuccess: () => toast.success('Temsilci atandı.'),
                      onError: (err) => toast.error(getErrorMessage(err)),
                    });
                  }}
                >
                  <option value="">Havuza al (atama yok)</option>
                  {(salesReps.data ?? []).map((rep) => (
                    <option key={rep.id} value={rep.id}>
                      {rep.name}
                    </option>
                  ))}
                </select>
              </div>
            )}
            <div className="detail-actions">
              {isLead && (
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={() => setConvertModal(true)}
                >
                  Müşteriye Dönüştür
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
                    updateCustomerStatus.mutate(nextStatus, {
                      onSuccess: () =>
                        toast.success(
                          nextStatus === 'Passive'
                            ? 'Müşteri pasif yapıldı.'
                            : 'Müşteri aktif yapıldı.',
                        ),
                      onError: (err) => toast.error(getErrorMessage(err)),
                    });
                  }}
                >
                  {updateCustomerStatus.isPending
                    ? 'Güncelleniyor...'
                    : data.customerStatus === 'Active'
                      ? 'Pasif Yap'
                      : 'Aktif Yap'}
                </button>
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
          </div>
        </div>

        <div className="detail-col">
          <div className="glass card">
            <div className="card-head">
              <h4>İlgili Kişiler</h4>
              <button
                type="button"
                className="btn btn-ghost btn-sm"
                onClick={() => {
                  // Yeni kişi ekle modunda aç
                  setEditingContact(undefined);
                  setContactModal(true);
                }}
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
                  {/* Düzenle / Sil aksiyonları */}
                  <div className="row-actions" style={{ marginTop: 8 }}>
                    <button
                      type="button"
                      className="btn btn-ghost btn-sm"
                      onClick={() => {
                        setEditingContact(contact);
                        setContactModal(true);
                      }}
                    >
                      Düzenle
                    </button>
                    <button
                      type="button"
                      className="btn btn-ghost btn-sm"
                      disabled={deleteContact.isPending}
                      onClick={() => void handleDeleteContact(contact)}
                    >
                      Sil
                    </button>
                  </div>
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
                                    updateStatus.mutate(
                                      {
                                        id: meeting.id,
                                        status: 'Done',
                                        companyId: id,
                                      },
                                      {
                                        onSuccess: () =>
                                          toast.success('Görüşme yapıldı olarak işaretlendi.'),
                                        onError: (err) =>
                                          toast.error(getErrorMessage(err)),
                                      },
                                    )
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

          <div className="glass card">
            <h4>Notlar &amp; Bekleyen İşler</h4>
            <div style={{ marginTop: 10 }}>
              <CompanyNotes companyId={id} />
            </div>
          </div>
        </div>
      </div>

      {ConfirmEl}

      {convertModal && (
        <ConvertToCustomerModal
          companyId={id}
          onClose={() => setConvertModal(false)}
        />
      )}
      {contactModal && (
        <ContactFormModal
          companyId={id}
          contact={editingContact}
          onClose={() => {
            setContactModal(false);
            setEditingContact(undefined);
          }}
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
            city: data.city ?? undefined,
            website: data.website ?? undefined,
            taxNumber: data.taxNumber ?? undefined,
          }}
          onClose={() => setOpportunityModal(false)}
        />
      )}
      {categoryModal && (
        <CategoryEditModal
          companyId={id}
          currentIds={data.categories.map((c) => c.id)}
          onClose={() => setCategoryModal(false)}
        />
      )}
    </>
  );
}

// ─── Kategori düzenleme modal'ı ─────────────────────────────────────────────

interface CategoryEditModalProps {
  companyId: string;
  currentIds: string[];
  onClose: () => void;
}

function CategoryEditModal({ companyId, currentIds, onClose }: CategoryEditModalProps) {
  const [selectedIds, setSelectedIds] = useState<string[]>(currentIds);
  const allCategories = useCategories();
  const setCompanyCategories = useSetCompanyCategories(companyId);
  const toast = useToast();

  async function handleSave() {
    try {
      await setCompanyCategories.mutateAsync(selectedIds);
      onClose();
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  }

  return (
    <Modal title="Kategorileri Düzenle" onClose={onClose} width={480}>
      <div style={{ padding: '8px 0 16px' }}>
        <p
          id="cat-edit-label"
          className="muted"
          style={{ fontSize: '0.85rem', marginBottom: 14 }}
        >
          Firmaya uygulamak istediğiniz kategorileri seçin.
        </p>
        {allCategories.isLoading && <Spinner />}
        {allCategories.data && (
          <CategoryMultiSelect
            categories={allCategories.data}
            value={selectedIds}
            onChange={setSelectedIds}
            groupLabelId="cat-edit-label"
          />
        )}
      </div>
      <div className="modal-footer">
        <button type="button" className="btn btn-ghost" onClick={onClose}>
          İptal
        </button>
        <button
          type="button"
          className="btn btn-primary"
          disabled={setCompanyCategories.isPending}
          onClick={() => void handleSave()}
        >
          {setCompanyCategories.isPending ? 'Kaydediliyor...' : 'Kaydet'}
        </button>
      </div>
    </Modal>
  );
}
