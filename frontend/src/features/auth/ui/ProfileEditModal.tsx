import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { FieldError } from '../../../shared/components/FieldError';
import { fieldAria } from '../../../shared/lib/fieldAria';
import { useToast } from '../../../shared/components/toast/ToastProvider';
import { applyServerFieldErrors } from '../../../shared/lib/applyServerFieldErrors';
import {
  updateProfileSchema,
  type UpdateProfileFormValues,
} from '../model/updateProfileSchema';
import { useUpdateProfile } from '../model/useUpdateProfile';
import type { UserDto } from '../../../entities/user/model/user';

interface ProfileEditModalProps {
  /** Formu önceden doldurmak için mevcut kullanıcı verisi */
  user: UserDto;
  onClose: () => void;
}

/**
 * Profil Düzenleme modalı — PATCH /auth/me.
 * Başarıda me sorgusu invalidate edilir (useUpdateProfile içinde).
 * fieldErrors RHF ile alanlara uygulanır.
 */
export function ProfileEditModal({ user, onClose }: ProfileEditModalProps) {
  const toast = useToast();
  const { mutateAsync, isPending } = useUpdateProfile();

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<UpdateProfileFormValues>({
    resolver: zodResolver(updateProfileSchema),
    // Mevcut değerlerle formu başlat
    defaultValues: {
      fullName: user.fullName,
      phone: user.phone ?? '',
      position: user.position ?? '',
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    // Boş string gönderilmemesi için undefined'a dönüştür
    const payload = {
      fullName: values.fullName,
      phone: values.phone || undefined,
      position: values.position || undefined,
    };

    try {
      await mutateAsync(payload);
      toast.success('Profil bilgileri güncellendi.');
      onClose();
    } catch (error) {
      // Backend 400 fieldErrors'ını alanlara uygula; genel hata toast'a düşer
      const generalMessage = applyServerFieldErrors(error, setError);
      if (generalMessage) {
        toast.error(generalMessage);
      }
    }
  });

  return (
    <Modal title="Profili Düzenle" onClose={onClose} width={480}>
      <form className="crm-form" onSubmit={onSubmit}>
        {/* Ad Soyad */}
        <div className="form-group">
          <label htmlFor="pe-fullName">Ad Soyad</label>
          <input
            id="pe-fullName"
            type="text"
            autoComplete="name"
            {...fieldAria('fullName', !!errors.fullName)}
            {...register('fullName')}
          />
          <FieldError id="fullName-error" message={errors.fullName?.message} />
        </div>

        {/* Telefon */}
        <div className="form-group">
          <label htmlFor="pe-phone">Telefon</label>
          <input
            id="pe-phone"
            type="tel"
            autoComplete="tel"
            placeholder="+90 5xx xxx xx xx"
            {...fieldAria('phone', !!errors.phone)}
            {...register('phone')}
          />
          <FieldError id="phone-error" message={errors.phone?.message} />
        </div>

        {/* Pozisyon */}
        <div className="form-group">
          <label htmlFor="pe-position">Pozisyon</label>
          <input
            id="pe-position"
            type="text"
            placeholder="Örn. Satış Uzmanı"
            {...fieldAria('position', !!errors.position)}
            {...register('position')}
          />
          <FieldError id="position-error" message={errors.position?.message} />
        </div>

        <div style={{ display: 'flex', gap: '0.5rem', marginTop: '1rem' }}>
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={onClose}
            disabled={isPending}
          >
            Vazgeç
          </button>
          <button
            type="submit"
            className="btn btn-primary"
            style={{ flex: 1 }}
            disabled={isPending}
          >
            {isPending ? 'Kaydediliyor...' : 'Kaydet'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
