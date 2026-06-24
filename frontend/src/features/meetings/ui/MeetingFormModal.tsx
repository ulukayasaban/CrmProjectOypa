import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { MEETING_METHOD_OPTIONS } from '../../../shared/constants/labels';
import { useToast } from '../../../shared/components/toast/ToastProvider';
import { applyServerFieldErrors } from '../../../shared/lib/applyServerFieldErrors';
import { useSalesReps } from '../../salesreps/model/useSalesReps';
import {
  useCompanyContacts,
  useCustomers,
  useLeads,
} from '../../companies/model/useCompanies';
import { useCreateMeeting, useUpdateMeeting } from '../model/useMeetings';
import { meetingSchema, type MeetingFormValues } from '../model/meetingSchema';
import type { CompanyDto } from '../../../entities/company/model/company';
import type { MeetingDto } from '../../../entities/meeting/model/meeting';

interface MeetingFormModalProps {
  onClose: () => void;
  /** Firma sabit tutulacaksa (şirket detay sayfası). */
  company?: CompanyDto;
  /** Yeni randevu için varsayılan tarih (takvim sayfası). */
  defaultDate?: string;
  /**
   * Mevcut görüşme geçilirse form DÜZENLE modunda açılır.
   * Geçilmezse YENİ OLUŞTUR modunda açılır.
   */
  meeting?: MeetingDto;
}

export function MeetingFormModal({
  onClose,
  company,
  defaultDate,
  meeting,
}: MeetingFormModalProps) {
  // Düzenleme mi yoksa oluşturma mı?
  const isEdit = meeting !== undefined;

  const createMeeting = useCreateMeeting();
  const updateMeeting = useUpdateMeeting();
  const salesReps = useSalesReps();
  const leads = useLeads();
  const customers = useCustomers();
  const toast = useToast();

  // Düzenleme modunda mevcut değerlerle doldur; oluşturma modunda varsayılanlar
  const defaultValues: MeetingFormValues = isEdit
    ? {
        companyId: meeting.companyId,
        contactId: meeting.contactId ?? '',
        salesRepId: meeting.salesRepId,
        date: meeting.date.slice(0, 10), // ISO → yyyy-MM-dd
        time: meeting.time.slice(0, 5),   // HH:mm:ss → HH:mm
        address: meeting.address,
        method: meeting.method,
      }
    : {
        companyId: company?.id ?? '',
        address: company?.address ?? '',
        date: defaultDate ?? '',
        contactId: '',
        salesRepId: '',
        time: '',
        method: 'Visit',
      };

  const {
    register,
    handleSubmit,
    watch,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<MeetingFormValues>({
    resolver: zodResolver(meetingSchema),
    defaultValues,
  });

  const selectedCompanyId = watch('companyId');
  const contacts = useCompanyContacts(selectedCompanyId);

  // Düzenleme modunda şirket listesi kilitli; oluşturma modunda tüm lead+müşteri
  const companyOptions: CompanyDto[] =
    company
      ? [company]
      : isEdit
        ? [] // Düzenleme modunda seçilen firmayı gösteremiyoruz (tam liste yüklenmez)
        : [...(leads.data ?? []), ...(customers.data ?? [])];

  const onSubmit = handleSubmit(async (values) => {
    // Backend HH:mm:ss bekler; <input type=time> HH:mm verir
    const timeFormatted =
      values.time.length === 5 ? `${values.time}:00` : values.time;

    try {
      if (isEdit) {
        // Güncelleme akışı: PUT /meetings/{id}
        await updateMeeting.mutateAsync({
          id: meeting.id,
          payload: {
            companyId: values.companyId,
            contactId: values.contactId || undefined,
            salesRepId: values.salesRepId,
            date: values.date,
            time: timeFormatted,
            address: values.address,
            method: values.method,
          },
        });
        toast.success('Randevu güncellendi.');
      } else {
        // Oluşturma akışı: POST /meetings
        await createMeeting.mutateAsync({
          companyId: values.companyId,
          contactId: values.contactId || undefined,
          salesRepId: values.salesRepId,
          date: values.date,
          time: timeFormatted,
          address: values.address,
          method: values.method,
        });
        toast.success('Randevu planlandı.');
      }
      onClose();
    } catch (err) {
      // Alan-bazlı sunucu hatalarını RHF'e uygula; kalan genel hatayı toast ile göster
      const generalMsg = applyServerFieldErrors<MeetingFormValues>(err, setError);
      if (generalMsg) {
        toast.error(generalMsg);
      }
    }
  });

  return (
    <Modal
      title={isEdit ? 'Randevuyu Düzenle' : 'Randevu / Görüşme Planla'}
      onClose={onClose}
      width={600}
    >
      <form className="crm-form" onSubmit={onSubmit}>
        <div className="form-group">
          <label htmlFor="companyId">Hedef Firma</label>
          <select
            id="companyId"
            defaultValue={isEdit ? meeting.companyId : (company?.id ?? '')}
            disabled={Boolean(company) || isEdit}
            {...register('companyId')}
          >
            <option value="" disabled>
              Seçiniz
            </option>
            {companyOptions.map((option) => (
              <option key={option.id} value={option.id}>
                {option.title}
              </option>
            ))}
            {/* Düzenleme modunda seçili firma adı göster */}
            {isEdit && (
              <option value={meeting.companyId}>{meeting.companyTitle}</option>
            )}
          </select>
          {errors.companyId && (
            <span className="field-error">{errors.companyId.message}</span>
          )}
        </div>
        <div className="form-group">
          <label htmlFor="contactId">Firma İlgili Kişisi</label>
          <select id="contactId" defaultValue={meeting?.contactId ?? ''} {...register('contactId')}>
            <option value="">Seçiniz (opsiyonel)</option>
            {(contacts.data ?? []).map((contact) => (
              <option key={contact.id} value={contact.id}>
                {contact.name}
              </option>
            ))}
          </select>
        </div>
        <div className="form-row">
          <div className="form-group">
            <label htmlFor="salesRepId">OYPA Temsilcisi</label>
            <select id="salesRepId" defaultValue={meeting?.salesRepId ?? ''} {...register('salesRepId')}>
              <option value="" disabled>
                Seçiniz
              </option>
              {(salesReps.data ?? []).map((rep) => (
                <option key={rep.id} value={rep.id}>
                  {rep.name}
                </option>
              ))}
            </select>
            {errors.salesRepId && (
              <span className="field-error">{errors.salesRepId.message}</span>
            )}
          </div>
          <div className="form-group">
            <label htmlFor="method">Yöntem</label>
            <select id="method" defaultValue={meeting?.method ?? 'Visit'} {...register('method')}>
              {MEETING_METHOD_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
            {errors.method && (
              <span className="field-error">{errors.method.message}</span>
            )}
          </div>
        </div>
        <div className="form-row">
          <div className="form-group">
            <label htmlFor="date">Tarih</label>
            <input id="date" type="date" {...register('date')} />
            {errors.date && (
              <span className="field-error">{errors.date.message}</span>
            )}
          </div>
          <div className="form-group">
            <label htmlFor="time">Saat</label>
            <input id="time" type="time" {...register('time')} />
            {errors.time && (
              <span className="field-error">{errors.time.message}</span>
            )}
          </div>
        </div>
        <div className="form-group">
          <label htmlFor="meeting-address">Görüşme Adresi</label>
          <input id="meeting-address" {...register('address')} />
          {errors.address && (
            <span className="field-error">{errors.address.message}</span>
          )}
        </div>
        <div className="modal-footer">
          <button type="button" className="btn btn-ghost" onClick={onClose}>
            İptal
          </button>
          <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
            {isSubmitting ? 'Kaydediliyor...' : 'Kaydet'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
