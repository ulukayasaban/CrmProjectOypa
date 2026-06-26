import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { FieldError } from '../../../shared/components/FieldError';
import { fieldAria } from '../../../shared/lib/fieldAria';
import { SECTOR_OPTIONS, SOURCE_OPTIONS } from '../../../shared/constants/labels';
import { useToast } from '../../../shared/components/toast/ToastProvider';
import { applyServerFieldErrors } from '../../../shared/lib/applyServerFieldErrors';
import { useCreateCompany } from '../model/useCompanies';
import {
  companySchema,
  type CompanyFormValues,
} from '../model/companySchema';
import type { CompanyDto } from '../../../entities/company/model/company';

/** Mevcut bir firmadan yeni fırsat oluştururken kopyalanabilecek alanlar. */
export interface CompanyPrefill {
  title: string;
  phone: string;
  email: string;
  address: string;
}

interface CompanyFormModalProps {
  onClose: () => void;
  onCreated?: (company: CompanyDto) => void;
  /** Verildiğinde modal "Yeni Fırsat" modunda açılır ve alanları kopyalama kutuları gösterir. */
  prefill?: CompanyPrefill;
}

type CopyField = 'phone' | 'email' | 'address';

const COPY_FIELDS: ReadonlyArray<{ field: CopyField; label: string; type?: string }> = [
  { field: 'phone', label: 'Telefon' },
  { field: 'email', label: 'E-posta', type: 'email' },
  { field: 'address', label: 'Adres' },
];

export function CompanyFormModal({ onClose, onCreated, prefill }: CompanyFormModalProps) {
  const createCompany = useCreateCompany();
  const toast = useToast();
  const isOpportunity = Boolean(prefill);
  const {
    register,
    handleSubmit,
    setValue,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<CompanyFormValues>({
    resolver: zodResolver(companySchema),
    defaultValues: prefill ? { title: prefill.title } : undefined,
  });
  const [copied, setCopied] = useState<Record<CopyField, boolean>>({
    phone: false,
    email: false,
    address: false,
  });

  function toggleCopy(field: CopyField) {
    const next = !copied[field];
    setCopied((current) => ({ ...current, [field]: next }));
    if (prefill) {
      setValue(field, next ? prefill[field] : '', { shouldValidate: true });
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
        source: values.source || undefined,
      });
      toast.success(isOpportunity ? 'Yeni fırsat oluşturuldu.' : 'Firma kaydedildi.');
      onCreated?.(company);
      onClose();
    } catch (err) {
      // Alan-bazlı sunucu hatalarını RHF'e uygula; kalan genel hatayı toast ile göster
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
        <div className="form-group">
          <label htmlFor="title">Firma Ünvanı</label>
          <input id="title" {...fieldAria('title', !!errors.title)} {...register('title')} />
          <FieldError id="title-error" message={errors.title?.message} />
        </div>
        <div className="form-group">
          <label htmlFor="sector">Sektör</label>
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
        {COPY_FIELDS.map(({ field, label, type }) => (
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
        <div className="form-group">
          <label htmlFor="city">Şehir</label>
          <input id="city" {...fieldAria('city', !!errors.city)} {...register('city')} />
          <FieldError id="city-error" message={errors.city?.message} />
        </div>
        <div className="form-group">
          <label htmlFor="website">Web Sitesi</label>
          <input id="website" type="url" placeholder="https://" {...fieldAria('website', !!errors.website)} {...register('website')} />
          <FieldError id="website-error" message={errors.website?.message} />
        </div>
        <div className="form-group">
          <label htmlFor="taxNumber">Vergi No</label>
          <input id="taxNumber" {...fieldAria('taxNumber', !!errors.taxNumber)} {...register('taxNumber')} />
          <FieldError id="taxNumber-error" message={errors.taxNumber?.message} />
        </div>
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
