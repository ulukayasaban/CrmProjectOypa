import { useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { FieldError } from '../../../shared/components/FieldError';
import { fieldAria } from '../../../shared/lib/fieldAria';
import {
  SECTOR_OPTIONS,
  SOURCE_OPTIONS,
  SERVICE_SECTOR_OPTIONS,
  FIRM_TYPE_OPTIONS,
} from '../../../shared/constants/labels';
import { useToast } from '../../../shared/components/toast/ToastProvider';
import { applyServerFieldErrors } from '../../../shared/lib/applyServerFieldErrors';
import { useCreateCompany } from '../model/useCompanies';
import {
  companySchema,
  type CompanyFormValues,
} from '../model/companySchema';
import { useSalesReps } from '../../salesreps/model/useSalesReps';
import type { CompanyDto } from '../../../entities/company/model/company';
import type { CompanySource, ServiceSector, FirmType } from '../../../shared/types/enums';

/** Mevcut bir firmadan yeni fırsat oluştururken kopyalanabilecek alanlar. */
export interface CompanyPrefill {
  title: string;
  phone: string;
  email: string;
  address: string;
  city?: string;
  website?: string;
  taxNumber?: string;
}

interface CompanyFormModalProps {
  onClose: () => void;
  onCreated?: (company: CompanyDto) => void;
  /** Verildiğinde modal "Yeni Fırsat" modunda açılır ve alanları kopyalama kutuları gösterir. */
  prefill?: CompanyPrefill;
}

type CopyField = 'phone' | 'email' | 'address' | 'city' | 'website' | 'taxNumber';

const COPY_FIELDS_REQUIRED: ReadonlyArray<{
  field: 'phone' | 'email' | 'address';
  label: string;
  type?: string;
}> = [
  { field: 'phone', label: 'Telefon' },
  { field: 'email', label: 'E-posta', type: 'email' },
  { field: 'address', label: 'Adres' },
];

const COPY_FIELDS_OPTIONAL: ReadonlyArray<{
  field: 'city' | 'website' | 'taxNumber';
  label: string;
  type?: string;
}> = [
  { field: 'city', label: 'Şehir' },
  { field: 'website', label: 'Web Sitesi', type: 'url' },
  { field: 'taxNumber', label: 'Vergi No' },
];

export function CompanyFormModal({ onClose, onCreated, prefill }: CompanyFormModalProps) {
  const createCompany = useCreateCompany();
  const toast = useToast();
  const salesReps = useSalesReps();
  const isOpportunity = Boolean(prefill);

  const {
    register,
    handleSubmit,
    setValue,
    setError,
    control,
    formState: { errors, isSubmitting },
  } = useForm<CompanyFormValues>({
    resolver: zodResolver(companySchema),
    defaultValues: prefill
      ? { title: prefill.title, firmType: 'DisFirma' }
      : { firmType: 'DisFirma' },
  });

  // Kaynak seçilince sourceNote alanını göster/gizle için izle
  const watchedSource = useWatch({ control, name: 'source' });
  const hasSource = Boolean(watchedSource);

  const [copied, setCopied] = useState<Record<CopyField, boolean>>({
    phone: false,
    email: false,
    address: false,
    city: false,
    website: false,
    taxNumber: false,
  });

  function toggleCopy(field: CopyField) {
    const next = !copied[field];
    setCopied((current) => ({ ...current, [field]: next }));
    if (prefill) {
      const prefillValue = prefill[field] ?? '';
      setValue(field, next ? prefillValue : '', { shouldValidate: true });
    }
  }

  const onSubmit = handleSubmit(async (values) => {
    try {
      const company = await createCompany.mutateAsync({
        title: values.title,
        sector: values.sector,
        phone: values.phone,
        email: values.email,
        address: values.address,
        city: values.city || undefined,
        website: values.website || undefined,
        taxNumber: values.taxNumber || undefined,
        source: (values.source || undefined) as CompanySource | undefined,
        sourceNote: values.sourceNote || undefined,
        serviceSector: (values.serviceSector || undefined) as ServiceSector | undefined,
        firmType: values.firmType as FirmType,
        leadOwnerId: values.leadOwnerId || undefined,
      });
      toast.success(isOpportunity ? 'Yeni fırsat oluşturuldu.' : 'Firma kaydedildi.');
      onCreated?.(company);
      onClose();
    } catch (err) {
      const generalMsg = applyServerFieldErrors<CompanyFormValues>(err, setError);
      if (generalMsg) {
        toast.error(generalMsg);
      }
    }
  });

  return (
    <Modal
      title={isOpportunity ? 'Yeni Fırsat Ekle' : 'Yeni Firma / Fırsat'}
      onClose={onClose}
      width={600}
    >
      <form className="crm-form" onSubmit={onSubmit}>
        {isOpportunity && (
          <p className="muted" style={{ fontSize: '0.8rem' }}>
            <strong>{prefill?.title}</strong> firması için yeni bir fırsat
            kaydı oluşturuluyor. Aynı iletişim bilgilerini kullanmak için
            kutuları işaretleyin.
          </p>
        )}

        {/* Firma Ünvanı */}
        <div className="form-group">
          <label htmlFor="title">Firma Ünvanı</label>
          <input id="title" {...fieldAria('title', !!errors.title)} {...register('title')} />
          <FieldError id="title-error" message={errors.title?.message} />
        </div>

        {/* Firma Sektörü (sector — Enerji vb.) */}
        <div className="form-group">
          <label htmlFor="sector">Firma Sektörü</label>
          <select id="sector" defaultValue="" {...fieldAria('sector', !!errors.sector)} {...register('sector')}>
            <option value="" disabled>
              Seçiniz
            </option>
            {SECTOR_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
          <FieldError id="sector-error" message={errors.sector?.message} />
        </div>

        {/* Firma Tipi */}
        <div className="form-group">
          <label htmlFor="firmType">Firma Tipi</label>
          <select id="firmType" {...fieldAria('firmType', !!errors.firmType)} {...register('firmType')}>
            {FIRM_TYPE_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
          <FieldError id="firmType-error" message={errors.firmType?.message} />
        </div>

        {/* Hizmet Verilecek Sektör (serviceSector — bizim sektörümüz) */}
        <div className="form-group">
          <label htmlFor="serviceSector">Hizmet Verilecek Sektör (Bizim)</label>
          <select id="serviceSector" defaultValue="" {...fieldAria('serviceSector', !!errors.serviceSector)} {...register('serviceSector')}>
            <option value="">Seçiniz (opsiyonel)</option>
            {SERVICE_SECTOR_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
          <FieldError id="serviceSector-error" message={errors.serviceSector?.message} />
        </div>

        {/* Zorunlu kopyalanabilir alanlar: telefon, e-posta, adres */}
        {COPY_FIELDS_REQUIRED.map(({ field, label, type }) => (
          <div className="form-group" key={field}>
            <label htmlFor={field}>{label}</label>
            <input
              id={field}
              type={type ?? 'text'}
              disabled={isOpportunity && copied[field]}
              {...fieldAria(field, !!errors[field])}
              {...register(field)}
            />
            {isOpportunity && (
              <label className="checkbox-inline">
                <input
                  type="checkbox"
                  checked={copied[field]}
                  onChange={() => toggleCopy(field)}
                />
                <span>Mevcutla aynı</span>
              </label>
            )}
            <FieldError id={`${field}-error`} message={errors[field]?.message} />
          </div>
        ))}

        {/* Opsiyonel kopyalanabilir alanlar: şehir, web sitesi, vergi no */}
        {COPY_FIELDS_OPTIONAL.map(({ field, label, type }) => (
          <div className="form-group" key={field}>
            <label htmlFor={field}>{label}</label>
            <input
              id={field}
              type={type ?? 'text'}
              placeholder={type === 'url' ? 'https://' : undefined}
              disabled={isOpportunity && copied[field] && Boolean(prefill?.[field])}
              {...fieldAria(field, !!errors[field])}
              {...register(field)}
            />
            {isOpportunity && prefill?.[field] && (
              <label className="checkbox-inline">
                <input
                  type="checkbox"
                  checked={copied[field]}
                  onChange={() => toggleCopy(field)}
                />
                <span>Mevcutla aynı</span>
              </label>
            )}
            <FieldError id={`${field}-error`} message={errors[field]?.message} />
          </div>
        ))}

        {/* Kaynak */}
        <div className="form-group">
          <label htmlFor="source">Kaynak</label>
          <select id="source" defaultValue="" {...fieldAria('source', !!errors.source)} {...register('source')}>
            <option value="">Seçiniz (opsiyonel)</option>
            {SOURCE_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
          <FieldError id="source-error" message={errors.source?.message} />
        </div>

        {/* Kaynak Notu — yalnızca kaynak seçiliyse görünür */}
        {hasSource && (
          <div className="form-group">
            <label htmlFor="sourceNote">Kaynak Notu</label>
            <input
              id="sourceNote"
              type="text"
              placeholder="Ör. Belgin Öner referansı"
              {...fieldAria('sourceNote', !!errors.sourceNote)}
              {...register('sourceNote')}
            />
            <FieldError id="sourceNote-error" message={errors.sourceNote?.message} />
          </div>
        )}

        {/* Atanan Lead (kim iletişim kurdu) */}
        <div className="form-group">
          <label htmlFor="leadOwnerId">Atanan Lead (kim iletişim kurdu)</label>
          <select
            id="leadOwnerId"
            defaultValue=""
            {...fieldAria('leadOwnerId', !!errors.leadOwnerId)}
            {...register('leadOwnerId')}
            disabled={salesReps.isLoading}
          >
            <option value="">Seçiniz (opsiyonel)</option>
            {(salesReps.data ?? []).map((rep) => (
              <option key={rep.id} value={rep.id}>
                {rep.name}
              </option>
            ))}
          </select>
          <FieldError id="leadOwnerId-error" message={errors.leadOwnerId?.message} />
        </div>

        <div className="modal-footer">
          <button type="button" className="btn btn-ghost" onClick={onClose}>
            İptal
          </button>
          <button
            type="submit"
            className="btn btn-primary"
            disabled={isSubmitting}
          >
            {isSubmitting ? 'Kaydediliyor...' : 'Kaydet'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
