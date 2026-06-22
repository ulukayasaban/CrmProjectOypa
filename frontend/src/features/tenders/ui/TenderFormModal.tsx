import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import {
  SECTOR_OPTIONS,
  TENDER_STATUS_OPTIONS,
} from '../../../shared/constants/labels';
import { getErrorMessage } from '../../../shared/lib/errorMessage';
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

  const {
    register,
    handleSubmit,
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
  const isError = isEdit ? updateTender.isError : createTender.isError;
  const mutationError = isEdit ? updateTender.error : createTender.error;

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

    if (isEdit && tender) {
      await updateTender.mutateAsync({ id: tender.id, payload });
    } else {
      await createTender.mutateAsync(payload);
    }
    onClose();
  });

  return (
    <Modal
      title={isEdit ? 'İhaleyi Düzenle' : 'Yeni İhale'}
      onClose={onClose}
      width={660}
    >
      <form className="crm-form" onSubmit={onSubmit}>
        {isError && (
          <div className="form-error">{getErrorMessage(mutationError)}</div>
        )}

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="companyId">Firma</label>
            <select id="companyId" defaultValue="" {...register('companyId')}>
              <option value="" disabled>
                Seçiniz
              </option>
              {companyOptions.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.title}
                </option>
              ))}
            </select>
            {errors.companyId && (
              <span className="field-error">{errors.companyId.message}</span>
            )}
          </div>
          <div className="form-group">
            <label htmlFor="sector">İş Kolu</label>
            <select id="sector" defaultValue="" {...register('sector')}>
              <option value="" disabled>
                Seçiniz
              </option>
              {SECTOR_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
            {errors.sector && (
              <span className="field-error">{errors.sector.message}</span>
            )}
          </div>
        </div>

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="title">İhale Başlığı</label>
            <input id="title" type="text" {...register('title')} />
            {errors.title && (
              <span className="field-error">{errors.title.message}</span>
            )}
          </div>
          <div className="form-group">
            <label htmlFor="tenderNumber">İhale No (opsiyonel)</label>
            <input id="tenderNumber" type="text" {...register('tenderNumber')} />
          </div>
        </div>

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="tenderDate">İhale Tarihi</label>
            <input id="tenderDate" type="date" {...register('tenderDate')} />
            {errors.tenderDate && (
              <span className="field-error">{errors.tenderDate.message}</span>
            )}
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
              {...register('personnelCount', { setValueAs: toNullableNumber })}
            />
            {errors.personnelCount && (
              <span className="field-error">
                {errors.personnelCount.message}
              </span>
            )}
          </div>
          <div className="form-group">
            <label htmlFor="estimatedValue">Tahmini Değer (₺)</label>
            <input
              id="estimatedValue"
              type="number"
              min={0}
              step={0.01}
              {...register('estimatedValue', { setValueAs: toNullableNumber })}
            />
            {errors.estimatedValue && (
              <span className="field-error">
                {errors.estimatedValue.message}
              </span>
            )}
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
              {...register('volume', { setValueAs: toNullableNumber })}
            />
            {errors.volume && (
              <span className="field-error">{errors.volume.message}</span>
            )}
          </div>
          <div className="form-group">
            <label htmlFor="quantity">Miktar</label>
            <input
              id="quantity"
              type="number"
              min={0}
              step={1}
              {...register('quantity', { setValueAs: toNullableNumber })}
            />
            {errors.quantity && (
              <span className="field-error">{errors.quantity.message}</span>
            )}
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
