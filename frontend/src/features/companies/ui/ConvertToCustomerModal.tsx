import { useForm } from 'react-hook-form';
import { Modal } from '../../../shared/components/Modal';
import { FieldError } from '../../../shared/components/FieldError';
import { fieldAria } from '../../../shared/lib/fieldAria';
import { SERVICE_SECTOR_OPTIONS } from '../../../shared/constants/labels';
import { useToast } from '../../../shared/components/toast/ToastProvider';
import { getErrorMessage } from '../../../shared/lib/errorMessage';
import { useConvertCompany } from '../model/useCompanies';
import { useSalesReps } from '../../salesreps/model/useSalesReps';
import type { ServiceSector } from '../../../shared/types/enums';

interface ConvertFormValues {
  salesRepId: string;
  serviceSector: ServiceSector | '';
  isNewCustomer: boolean;
}

interface ConvertToCustomerModalProps {
  companyId: string;
  onClose: () => void;
}

export function ConvertToCustomerModal({
  companyId,
  onClose,
}: ConvertToCustomerModalProps) {
  const toast = useToast();
  const salesReps = useSalesReps();
  const convert = useConvertCompany(companyId);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ConvertFormValues>({
    defaultValues: {
      salesRepId: '',
      serviceSector: '',
      isNewCustomer: false,
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await convert.mutateAsync({
        salesRepId: values.salesRepId === '' ? null : values.salesRepId,
        serviceSector:
          values.serviceSector === ''
            ? null
            : (values.serviceSector as ServiceSector),
        isNewCustomer: values.isNewCustomer,
      });
      toast.success('Firma müşteriye dönüştürüldü.');
      onClose();
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  });

  return (
    <Modal title="Müşteriye Dönüştür" onClose={onClose} width={480}>
      <form className="crm-form" onSubmit={onSubmit}>
        {/* Hizmet Sektörü */}
        <div className="form-group">
          <label htmlFor="convert-serviceSector">
            Hizmet Verilecek Sektör (opsiyonel)
          </label>
          <select
            id="convert-serviceSector"
            defaultValue=""
            {...fieldAria('convert-serviceSector', !!errors.serviceSector)}
            {...register('serviceSector')}
          >
            <option value="">Seçiniz (opsiyonel)</option>
            {SERVICE_SECTOR_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
          <FieldError
            id="convert-serviceSector-error"
            message={errors.serviceSector?.message}
          />
        </div>

        {/* Satış Temsilcisi */}
        <div className="form-group">
          <label htmlFor="convert-salesRepId">Satış Temsilcisi (opsiyonel)</label>
          <select
            id="convert-salesRepId"
            {...fieldAria('convert-salesRepId', !!errors.salesRepId)}
            {...register('salesRepId')}
            disabled={salesReps.isLoading}
          >
            <option value="">Havuz (atanmamış)</option>
            {(salesReps.data ?? []).map((rep) => (
              <option key={rep.id} value={rep.id}>
                {rep.name}
              </option>
            ))}
          </select>
          <FieldError
            id="convert-salesRepId-error"
            message={errors.salesRepId?.message}
          />
        </div>

        {/* Yeni Müşteri checkbox */}
        <div className="form-group">
          <label className="checkbox-inline" style={{ cursor: 'pointer' }}>
            <input
              type="checkbox"
              {...register('isNewCustomer')}
              style={{ marginRight: 8 }}
            />
            <span>Yeni müşteri olarak işaretle</span>
          </label>
        </div>

        <div className="modal-footer">
          <button type="button" className="btn btn-ghost" onClick={onClose}>
            Vazgeç
          </button>
          <button
            type="submit"
            className="btn btn-primary"
            disabled={isSubmitting || convert.isPending}
          >
            {isSubmitting || convert.isPending
              ? 'Dönüştürülüyor...'
              : 'Dönüştür'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
