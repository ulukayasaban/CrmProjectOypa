import { useSearchParams, useNavigate, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  resetPasswordSchema,
  type ResetPasswordFormValues,
} from '../features/auth/model/resetPasswordSchema';
import { useResetPassword } from '../features/auth/model/useResetPassword';
import { applyServerFieldErrors } from '../shared/lib/applyServerFieldErrors';
import { useToast } from '../shared/components/toast/ToastProvider';

/**
 * Parola Sıfırlama sayfası — anonim erişim.
 * URL query parametrelerinden email ve token okunur.
 * Örnek: /reset-password?email=a@b.com&token=xyz
 */
export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const toast = useToast();
  const { mutateAsync, isPending } = useResetPassword();

  // E-posta ve token query string'den alınır
  const email = searchParams.get('email') ?? '';
  const token = searchParams.get('token') ?? '';

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { newPassword: '', confirmPassword: '' },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutateAsync({ email, token, newPassword: values.newPassword });
      toast.success('Parolanız başarıyla sıfırlandı. Giriş yapabilirsiniz.');
      navigate('/login', { replace: true });
    } catch (error) {
      // Backend fieldErrors varsa alanlara uygula, yoksa genel toast
      const generalMessage = applyServerFieldErrors(error, setError);
      if (generalMessage) {
        toast.error(generalMessage);
      }
    }
  });

  // Token veya e-posta eksikse geçersiz bağlantı uyarısı
  if (!email || !token) {
    return (
      <div className="center-screen">
        <div className="login-card glass">
          <div className="login-logo">
            <h1>
              OYPA<span>CRM</span>
            </h1>
          </div>
          <div style={{ textAlign: 'center', padding: '1rem 0' }}>
            <p style={{ color: 'var(--danger, #e74c3c)', marginBottom: '1rem' }}>
              Geçersiz veya eksik sıfırlama bağlantısı.
              Lütfen yeni bir sıfırlama talebinde bulunun.
            </p>
            <Link to="/forgot-password" className="btn btn-primary btn-sm">
              Yeni Bağlantı İste
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="center-screen">
      <div className="login-card glass">
        <div className="login-logo">
          <h1>
            OYPA<span>CRM</span>
          </h1>
          <p>Yeni Parola Belirle</p>
        </div>
        <form className="crm-form" onSubmit={onSubmit}>
          <p style={{ marginBottom: '1rem', opacity: 0.8, fontSize: '0.9rem' }}>
            <strong>{email}</strong> hesabı için yeni parola belirleyin.
          </p>
          <div className="form-group">
            <label htmlFor="newPassword">Yeni Parola</label>
            <input
              id="newPassword"
              type="password"
              autoComplete="new-password"
              placeholder="En az 8 karakter"
              {...register('newPassword')}
            />
            {errors.newPassword && (
              <span className="field-error">{errors.newPassword.message}</span>
            )}
          </div>
          <div className="form-group">
            <label htmlFor="confirmPassword">Yeni Parola (Tekrar)</label>
            <input
              id="confirmPassword"
              type="password"
              autoComplete="new-password"
              placeholder="Parolayı tekrar girin"
              {...register('confirmPassword')}
            />
            {errors.confirmPassword && (
              <span className="field-error">{errors.confirmPassword.message}</span>
            )}
          </div>
          <button
            type="submit"
            className="btn btn-primary btn-block"
            disabled={isPending}
          >
            {isPending ? 'Sıfırlanıyor...' : 'Parolayı Sıfırla'}
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
      </div>
    </div>
  );
}
