import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useAuth } from '../app/providers/useAuth';
import {
  loginSchema,
  type LoginFormValues,
} from '../features/auth/model/loginSchema';
import { FieldError } from '../shared/components/FieldError';
import { OyakLogo } from '../shared/components/OyakLogo';
import { fieldAria } from '../shared/lib/fieldAria';
import { getErrorMessage } from '../shared/lib/errorMessage';

interface LocationState {
  from?: string;
}

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      await login(values);
      const state = location.state as LocationState | null;
      navigate(state?.from ?? '/', { replace: true });
    } catch (error) {
      setFormError(getErrorMessage(error));
    }
  });

  return (
    <div className="center-screen">
      <div className="login-card glass">
        <div className="login-logo">
          <OyakLogo height={44} />
          <p>Satış ve Pazarlama Yönetim Platformu</p>
        </div>
        <form className="crm-form" onSubmit={onSubmit}>
          {formError && <div className="form-error">{formError}</div>}
          <div className="form-group">
            <label htmlFor="email">E-posta</label>
            <input
              id="email"
              type="email"
              autoComplete="username"
              placeholder="admin@oypa.com.tr"
              {...fieldAria('email', !!errors.email)}
              {...register('email')}
            />
            <FieldError id="email-error" message={errors.email?.message} />
          </div>
          <div className="form-group">
            <label htmlFor="password">Parola</label>
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              {...fieldAria('password', !!errors.password)}
              {...register('password')}
            />
            <FieldError id="password-error" message={errors.password?.message} />
          </div>

          {/* Şifremi Unuttum linki — sağa yaslanmış, parola alanının altında */}
          <div style={{ textAlign: 'right', marginTop: '-0.25rem', marginBottom: '0.75rem' }}>
            <Link
              to="/forgot-password"
              style={{ color: 'var(--accent-gold)', fontSize: '0.85rem' }}
            >
              Şifremi Unuttum?
            </Link>
          </div>

          <button
            type="submit"
            className="btn btn-primary btn-block"
            disabled={isSubmitting}
          >
            {isSubmitting ? 'Giriş yapılıyor...' : 'Giriş Yap'}
          </button>
        </form>
      </div>
    </div>
  );
}
