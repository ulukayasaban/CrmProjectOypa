import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { FieldError } from '../../../shared/components/FieldError';
import { fieldAria } from '../../../shared/lib/fieldAria';
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
    setValue,
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
            {...fieldAria('companyId', !!errors.companyId)}
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
          <FieldError id="companyId-error" message={errors.companyId?.message} />
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
            <select id="salesRepId" defaultValue={meeting?.salesRepId ?? ''} {...fieldAria('salesRepId', !!errors.salesRepId)} {...register('salesRepId')}>
              <option value="" disabled>
                Seçiniz
              </option>
              {(salesReps.data ?? []).map((rep) => (
                <option key={rep.id} value={rep.id}>
                  {rep.name}
                </option>
              ))}
            </select>
            <FieldError id="salesRepId-error" message={errors.salesRepId?.message} />
          </div>
          <div className="form-group">
            <label htmlFor="method">Yöntem</label>
            <select id="method" defaultValue={meeting?.method ?? 'Visit'} {...fieldAria('method', !!errors.method)} {...register('method')}>
              {MEETING_METHOD_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
            <FieldError id="method-error" message={errors.method?.message} />
          </div>
        </div>
        <div className="form-row">
          <div className="form-group">
            <label htmlFor="date">Tarih</label>
            <input
              id="date"
              type="date"
              {...fieldAria('date', !!errors.date)}
              {...register('date')}
              onClick={(e) => {
                // Alanın herhangi bir yerine tıklayınca native picker açılsın
                // (yalnız minik takvim ikonu değil). Desteklenmiyorsa yoksay.
                try {
                  e.currentTarget.showPicker();
                } catch {
                  /* showPicker desteklenmiyor — varsayılan davranış geçerli */
                }
              }}
            />
            <FieldError id="date-error" message={errors.date?.message} />
          </div>
          <div className="form-group">
            <label htmlFor="time">Saat</label>
            <input
              id="time"
              type="time"
              {...fieldAria('time', !!errors.time)}
              {...register('time')}
              onClick={(e) => {
                try {
                  e.currentTarget.showPicker();
                } catch {
                  /* showPicker desteklenmiyor — varsayılan davranış geçerli */
                }
              }}
            />
            <FieldError id="time-error" message={errors.time?.message} />
          </div>
        </div>
        <div className="form-group">
          <label htmlFor="meeting-address">Görüşme Adresi</label>
          <input id="meeting-address" {...fieldAria('address', !!errors.address)} {...register('address')} />
          <FieldError id="address-error" message={errors.address?.message} />
          {/* Hızlı adres doldurma butonları */}
          <div style={{ display: 'flex', gap: 4, flexWrap: 'wrap', marginTop: 4 }}>
            {(() => {
              // Seçili firmayı bul: prop'tan gelen company öncelikli,
              // yoksa dropdown'da seçili olanı companyOptions'tan eşleştir.
              const selectedCompany =
                company ??
                companyOptions.find((c) => c.id === selectedCompanyId);
              const companyAddress = selectedCompany?.address ?? '';
              return (
                <button
                  type="button"
                  className="btn btn-ghost btn-sm"
                  disabled={!companyAddress}
                  onClick={() => setValue('address', companyAddress)}
                >
                  Firma Adresi
                </button>
              );
            })()}
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              onClick={() => setValue('address', 'OYAK Ankara')}
            >
              OYAK Ankara
            </button>
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              onClick={() => setValue('address', 'OYAK Dragos')}
            >
              OYAK Dragos
            </button>
          </div>
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
