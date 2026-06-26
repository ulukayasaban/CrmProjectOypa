import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  forgotPasswordSchema,
  type ForgotPasswordFormValues,
} from '../features/auth/model/forgotPasswordSchema';
import { useForgotPassword } from '../features/auth/model/useForgotPassword';
import { FieldError } from '../shared/components/FieldError';
import { fieldAria } from '../shared/lib/fieldAria';
import { useToast } from '../shared/components/toast/ToastProvider';
import { getErrorMessage } from '../shared/lib/errorMessage';

/**
 * Şifremi Unuttum sayfası — anonim erişim.
 * Backend güvenlik gereği her durumda 200 döner; bilgi sızdırma önlenir.
 */
export default function ForgotPasswordPage() {
  const toast = useToast();
  const { mutateAsync, isPending } = useForgotPassword();
  // Başarılı gönderi sonrası nötr bilgi mesajı göstermek için
  const [submitted, setSubmitted] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: '' },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutateAsync(values);
      setSubmitted(true);
      toast.success('İstek gönderildi.');
    } catch (error) {
      // Beklenmedik ağ hataları için toast — backend zaten 200 döner
      toast.error(getErrorMessage(error));
    }
  });

  return (
    <div className="center-screen">
      <div className="login-card glass">
        <div className="login-logo">
          <h1>
            OYPA<span>CRM</span>
          </h1>
          <p>Parola Sıfırlama</p>
        </div>

        {submitted ? (
          /* Güvenli nötr mesaj — e-postanın kayıtlı olup olmadığını açıklamaz */
          <div style={{ textAlign: 'center', padding: '1rem 0' }}>
            <p style={{ color: 'var(--accent-gold)', marginBottom: '1rem' }}>
              Eğer bu e-posta kayıtlıysa sıfırlama bağlantısı gönderildi.
            </p>
            <Link to="/login" className="btn btn-ghost btn-sm">
              Girişe Dön
            </Link>
          </div>
        ) : (
          <form className="crm-form" onSubmit={onSubmit}>
            <p style={{ marginBottom: '1rem', opacity: 0.8, fontSize: '0.9rem' }}>
              Kayıtlı e-posta adresinizi girin; sıfırlama bağlantısı göndereceğiz.
            </p>
            <div className="form-group">
              <label htmlFor="email">E-posta</label>
              <input
                id="email"
                type="email"
                autoComplete="email"
                placeholder="ornek@oypa.com.tr"
                {...fieldAria('email', !!errors.email)}
                {...register('email')}
              />
              <FieldError id="email-error" message={errors.email?.message} />
            </div>
            <button
              type="submit"
              className="btn btn-primary btn-block"
              disabled={isPending}
            >
              {isPending ? 'Gönderiliyor...' : 'Sıfırlama Bağlantısı Gönder'}
            </button>
            <div style={{ textAlign: 'center', marginTop: '1rem' }}>
              <Link
                to="/login"
                style={{ color: 'var(--accent-gold)', fontSize: '0.9rem' }}
              >
                Girişe Dön
              </Link>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
