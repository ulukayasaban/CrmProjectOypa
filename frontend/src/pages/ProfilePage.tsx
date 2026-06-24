import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { authApi } from '../features/auth/api/authApi';
import { queryKeys } from '../shared/api/queryKeys';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { getErrorMessage } from '../shared/lib/errorMessage';
import { ChangePasswordModal } from '../features/auth/ui/ChangePasswordModal';
import { ProfileEditModal } from '../features/auth/ui/ProfileEditModal';

/**
 * Profil sayfası — giriş yapmış kullanıcının bilgilerini gösterir.
 * "Profili Düzenle" → ProfileEditModal (PATCH /auth/me)
 * "Parolayı Değiştir" → ChangePasswordModal (POST /auth/change-password)
 */
export default function ProfilePage() {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: queryKeys.me,
    queryFn: authApi.me,
  });

  // Modal görünürlük durumları
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [isChangePasswordOpen, setIsChangePasswordOpen] = useState(false);

  if (isLoading) return <Spinner />;
  if (isError || !data) return <StateBlock message={getErrorMessage(error)} />;

  return (
    <>
      <div className="glass profile-card">
        {/* Avatar — ad soyadının ilk harfi */}
        <div className="avatar profile-avatar">{data.fullName[0]}</div>
        <h2>{data.fullName}</h2>
        <p style={{ color: 'var(--accent-gold)', marginBottom: 20 }}>
          {data.position ?? data.roles.join(', ')}
        </p>

        {/* Profil bilgileri */}
        <div className="profile-info">
          <span>
            📧 <strong>E-posta:</strong> {data.email}
          </span>
          <span>
            📞 <strong>Telefon:</strong> {data.phone ?? '-'}
          </span>
          <span>
            🛡️ <strong>Roller:</strong> {data.roles.join(', ') || '-'}
          </span>
        </div>

        {/* Eylem butonları */}
        <div
          style={{
            display: 'flex',
            gap: '0.75rem',
            marginTop: '1.5rem',
            justifyContent: 'center',
            flexWrap: 'wrap',
          }}
        >
          <button
            type="button"
            className="btn btn-primary btn-sm"
            onClick={() => setIsEditOpen(true)}
          >
            Profili Düzenle
          </button>
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={() => setIsChangePasswordOpen(true)}
          >
            Parolayı Değiştir
          </button>
        </div>
      </div>

      {/* Profil düzenleme modalı */}
      {isEditOpen && (
        <ProfileEditModal
          user={data}
          onClose={() => setIsEditOpen(false)}
        />
      )}

      {/* Parola değiştirme modalı */}
      {isChangePasswordOpen && (
        <ChangePasswordModal onClose={() => setIsChangePasswordOpen(false)} />
      )}
    </>
  );
}
