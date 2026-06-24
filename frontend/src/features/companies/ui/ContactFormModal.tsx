import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { useToast } from '../../../shared/components/toast/ToastProvider';
import { applyServerFieldErrors } from '../../../shared/lib/applyServerFieldErrors';
import { useCreateContact } from '../model/useCompanies';
import { contactSchema, type ContactFormValues } from '../model/companySchema';

interface ContactFormModalProps {
  companyId: string;
  onClose: () => void;
}

export function ContactFormModal({ companyId, onClose }: ContactFormModalProps) {
  const createContact = useCreateContact(companyId);
  const toast = useToast();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ContactFormValues>({
    resolver: zodResolver(contactSchema),
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await createContact.mutateAsync({
        name: values.name,
        email: values.email || undefined,
        phone: values.phone || undefined,
      });
      toast.success('İlgili kişi eklendi.');
      onClose();
    } catch (err) {
      const generalMsg = applyServerFieldErrors<ContactFormValues>(err, setError);
      if (generalMsg) {
        toast.error(generalMsg);
      }
    }
  });

  return (
    <Modal title="Yeni İlgili Kişi" onClose={onClose}>
      <form className="crm-form" onSubmit={onSubmit}>
        <div className="form-group">
          <label htmlFor="name">İsim Soyisim</label>
          <input id="name" {...register('name')} />
          {errors.name && (
            <span className="field-error">{errors.name.message}</span>
          )}
        </div>
        <div className="form-group">
          <label htmlFor="contact-email">E-posta</label>
          <input id="contact-email" type="email" {...register('email')} />
          {errors.email && (
            <span className="field-error">{errors.email.message}</span>
          )}
        </div>
        <div className="form-group">
          <label htmlFor="contact-phone">Telefon</label>
          <input id="contact-phone" {...register('phone')} />
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
