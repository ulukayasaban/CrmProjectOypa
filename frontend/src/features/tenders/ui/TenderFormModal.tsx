import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { FieldError } from '../../../shared/components/FieldError';
import { fieldAria } from '../../../shared/lib/fieldAria';
import {
  SECTOR_OPTIONS,
  TENDER_STATUS_OPTIONS,
} from '../../../shared/constants/labels';
import { useToast } from '../../../shared/components/toast/ToastProvider';
import { applyServerFieldErrors } from '../../../shared/lib/applyServerFieldErrors';
import { useSalesReps } from '../../salesreps/model/useSalesReps';
import { useCustomers, useLeads } from '../../companies/model/useCompanies';
import { useCreateTender, useUpdateTender } from '../model/useTenders';
import { tenderSchema, type TenderFormValues } from '../model/tenderSchema';
import type { TenderDto } from '../../../entities/tender/model/tender';

interface TenderFormModalProps {
  onClose: () => void;
  /** When set, the modal is in edit mode. */
  tender?: TenderDto;
}

/** Boş number input'u `null`'a çevirir; aksi halde `valueAsNumber` NaN üretirdi. */
const toNullableNumber = (value: string): number | null => {
  if (value === '') return null;
  const n = Number(value);
  return Number.isNaN(n) ? null : n;
};

export function TenderFormModal({ onClose, tender }: TenderFormModalProps) {
  const isEdit = Boolean(tender);
  const createTender = useCreateTender();
  const updateTender = useUpdateTender();
  const salesReps = useSalesReps();
  const leads = useLeads();
  const customers = useCustomers();
  const toast = useToast();

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<TenderFormValues>({
    resolver: zodResolver(tenderSchema),
    defaultValues: tender
      ? {
          companyId: tender.companyId,
          title: tender.title,
          tenderNumber: tender.tenderNumber ?? '',
          sector: tender.sector,
          tenderDate: tender.tenderDate.slice(0, 10),
          status: tender.status,
          personnelCount: tender.personnelCount ?? undefined,
          estimatedValue: tender.estimatedValue ?? undefined,
          volume: tender.volume ?? undefined,
          quantity: tender.quantity ?? undefined,
          description: tender.description ?? '',
          assignedSalesRepId: tender.assignedSalesRepId ?? '',
        }
      : {
          companyId: '',
          title: '',
          tenderNumber: '',
          sector: undefined,
          tenderDate: '',
          description: '',
          assignedSalesRepId: '',
        },
  });

  const companyOptions = [...(leads.data ?? []), ...(customers.data ?? [])];

  const isPending = isEdit ? updateTender.isPending : createTender.isPending;

  const onSubmit = handleSubmit(async (values) => {
    const payload = {
      companyId: values.companyId,
      title: values.title,
      tenderNumber: values.tenderNumber || null,
      sector: values.sector,
      tenderDate: values.tenderDate,
      status: values.status,
      personnelCount: values.personnelCount ?? null,
      estimatedValue: values.estimatedValue ?? null,
      volume: values.volume ?? null,
      quantity: values.quantity ?? null,
      description: values.description || null,
      assignedSalesRepId: values.assignedSalesRepId || null,
    };

    try {
      if (isEdit && tender) {
        await updateTender.mutateAsync({ id: tender.id, payload });
        toast.success('İhale güncellendi.');
      } else {
        await createTender.mutateAsync(payload);
        toast.success('İhale kaydedildi.');
      }
      onClose();
    } catch (err) {
      // Alan-bazlı sunucu hatalarını RHF'e uygula; kalan genel hatayı toast ile göster
      const generalMsg = applyServerFieldErrors<TenderFormValues>(err, setError);
      if (generalMsg) {
        toast.error(generalMsg);
      }
    }
  });

  return (
    <Modal
      title={isEdit ? 'İhaleyi Düzenle' : 'Yeni İhale'}
      onClose={onClose}
      width={660}
    >
      <form className="crm-form" onSubmit={onSubmit}>
        <div className="form-row">
          <div className="form-group">
            <label htmlFor="companyId">Firma</label>
            <select id="companyId" defaultValue="" {...fieldAria('companyId', !!errors.companyId)} {...register('companyId')}>
              <option value="" disabled>
                Seçiniz
              </option>
              {companyOptions.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.title}
                </option>
              ))}
            </select>
            <FieldError id="companyId-error" message={errors.companyId?.message} />
          </div>
          <div className="form-group">
            <label htmlFor="sector">İş Kolu</label>
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
        </div>

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="title">İhale Başlığı</label>
            <input id="title" type="text" {...fieldAria('title', !!errors.title)} {...register('title')} />
            <FieldError id="title-error" message={errors.title?.message} />
          </div>
          <div className="form-group">
            <label htmlFor="tenderNumber">İhale No (opsiyonel)</label>
            <input id="tenderNumber" type="text" {...register('tenderNumber')} />
          </div>
        </div>

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="tenderDate">İhale Tarihi</label>
            <input id="tenderDate" type="date" {...fieldAria('tenderDate', !!errors.tenderDate)} {...register('tenderDate')} />
            <FieldError id="tenderDate-error" message={errors.tenderDate?.message} />
          </div>
          {isEdit && (
            <div className="form-group">
              <label htmlFor="status">Durum</label>
              <select id="status" {...register('status')}>
                {TENDER_STATUS_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>
          )}
        </div>

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="personnelCount">Personel Sayısı</label>
            <input
              id="personnelCount"
              type="number"
              min={0}
              step={1}
              {...fieldAria('personnelCount', !!errors.personnelCount)}
              {...register('personnelCount', { setValueAs: toNullableNumber })}
            />
            <FieldError id="personnelCount-error" message={errors.personnelCount?.message} />
          </div>
          <div className="form-group">
            <label htmlFor="estimatedValue">Tahmini Değer (₺)</label>
            <input
              id="estimatedValue"
              type="number"
              min={0}
              step={0.01}
              {...fieldAria('estimatedValue', !!errors.estimatedValue)}
              {...register('estimatedValue', { setValueAs: toNullableNumber })}
            />
            <FieldError id="estimatedValue-error" message={errors.estimatedValue?.message} />
          </div>
        </div>

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="volume">Hacim</label>
            <input
              id="volume"
              type="number"
              min={0}
              step={0.01}
              {...fieldAria('volume', !!errors.volume)}
              {...register('volume', { setValueAs: toNullableNumber })}
            />
            <FieldError id="volume-error" message={errors.volume?.message} />
          </div>
          <div className="form-group">
            <label htmlFor="quantity">Miktar</label>
            <input
              id="quantity"
              type="number"
              min={0}
              step={1}
              {...fieldAria('quantity', !!errors.quantity)}
              {...register('quantity', { setValueAs: toNullableNumber })}
            />
            <FieldError id="quantity-error" message={errors.quantity?.message} />
          </div>
        </div>

        <div className="form-group">
          <label htmlFor="assignedSalesRepId">Sorumlu Temsilci</label>
          <select
            id="assignedSalesRepId"
            defaultValue=""
            {...register('assignedSalesRepId')}
          >
            <option value="">Seçiniz (opsiyonel)</option>
            {(salesReps.data ?? []).map((rep) => (
              <option key={rep.id} value={rep.id}>
                {rep.name}
              </option>
            ))}
          </select>
        </div>

        <div className="form-group">
          <label htmlFor="description">Açıklama</label>
          <textarea
            id="description"
            rows={3}
            {...register('description')}
          />
        </div>

        <div className="modal-footer">
          <button type="button" className="btn btn-ghost" onClick={onClose}>
            İptal
          </button>
          <button
            type="submit"
            className="btn btn-primary"
            disabled={isSubmitting || isPending}
          >
            {isSubmitting || isPending ? 'Kaydediliyor...' : 'Kaydet'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
