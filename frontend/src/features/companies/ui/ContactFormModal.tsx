import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { useToast } from '../../../shared/components/toast/ToastProvider';
import { applyServerFieldErrors } from '../../../shared/lib/applyServerFieldErrors';
import {
  useCreateContact,
  useUpdateContact,
} from '../model/useCompanies';
import { contactSchema, type ContactFormValues } from '../model/companySchema';
import type { ContactDto } from '../../../entities/company/model/company';

interface ContactFormModalProps {
  companyId: string;
  onClose: () => void;
  /**
   * Mevcut kişi geçilirse form DÜZENLE modunda açılır.
   * Geçilmezse YENİ EKLE modunda açılır.
   */
  contact?: ContactDto;
}

export function ContactFormModal({
  companyId,
  onClose,
  contact,
}: ContactFormModalProps) {
  // Düzenleme mi yoksa oluşturma mı olduğunu belirle
  const isEdit = contact !== undefined;

  const createContact = useCreateContact(companyId);
  const updateContact = useUpdateContact(companyId);
  const toast = useToast();

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ContactFormValues>({
    resolver: zodResolver(contactSchema),
    // Düzenleme modunda mevcut değerleri ön-doldur
    defaultValues: isEdit
      ? {
          name: contact.name,
          email: contact.email ?? '',
          phone: contact.phone ?? '',
        }
      : undefined,
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      if (isEdit) {
        // Güncelleme akışı: PUT /contacts/{id}
        await updateContact.mutateAsync({
          contactId: contact.id,
          payload: {
            name: values.name,
            email: values.email || undefined,
            phone: values.phone || undefined,
          },
        });
        toast.success('İlgili kişi güncellendi.');
      } else {
        // Oluşturma akışı: POST /companies/{id}/contacts
        await createContact.mutateAsync({
          name: values.name,
          email: values.email || undefined,
          phone: values.phone || undefined,
        });
        toast.success('İlgili kişi eklendi.');
      }
      onClose();
    } catch (err) {
      const generalMsg = applyServerFieldErrors<ContactFormValues>(err, setError);
      if (generalMsg) {
        toast.error(generalMsg);
      }
    }
  });

  return (
    <Modal
      title={isEdit ? 'İlgili Kişiyi Düzenle' : 'Yeni İlgili Kişi'}
      onClose={onClose}
    >
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
