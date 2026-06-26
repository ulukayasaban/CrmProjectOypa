import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { authApi } from '../features/auth/api/authApi';
import { useCustomers } from '../features/companies/model/useCompanies';
import { queryKeys } from '../shared/api/queryKeys';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { getErrorMessage } from '../shared/lib/errorMessage';
import { ChangePasswordModal } from '../features/auth/ui/ChangePasswordModal';
import { ProfileEditModal } from '../features/auth/ui/ProfileEditModal';
import { SECTOR_LABELS, CUSTOMER_STATUS_LABELS } from '../shared/constants/labels';

/**
 * Profil sayfası — giriş yapmış kullanıcının bilgilerini + portföyünü gösterir.
 * "Profili Düzenle" → ProfileEditModal (PATCH /auth/me)
 * "Parolayı Değiştir" → ChangePasswordModal (POST /auth/change-password)
 *
 * Portföy: CompanyDto.assignedSalesRepName ile kullanıcının fullName'i eşleştirilerek
 * client-side filtrelenir. Backend /salesreps ucu ile rep ID eşleşmesi yapılamamaktadır
 * çünkü UserDto'da salesRepId alanı bulunmamaktadır. Bu bilgi için backend ajanına
 * bildirim yapılmıştır (bakınız rapor).
 */
export default function ProfilePage() {
  const navigate = useNavigate();

  const { data, isLoading, isError, error } = useQuery({
    queryKey: queryKeys.me,
    queryFn: authApi.me,
  });

  // Müşteri portföyü için tüm müşteri listesi (tam liste, sayfalı değil)
  const customersQuery = useCustomers();

  // Modal görünürlük durumları
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [isChangePasswordOpen, setIsChangePasswordOpen] = useState(false);

  // Giriş yapan kullanıcının portföyü: kullanıcının SalesRep id'si ile firmanın
  // atanan temsilci id'si KESİN eşleşmesi. assignedSalesRepId yoksa portföy boştur.
  const myPortfolio = useMemo(() => {
    if (!data?.assignedSalesRepId || !customersQuery.data) return [];
    return customersQuery.data.filter(
      (c) => c.assignedSalesRepId === data.assignedSalesRepId,
    );
  }, [data, customersQuery.data]);

  if (isLoading || customersQuery.isLoading) return <Spinner />;
  if (isError || !data) return <StateBlock message={getErrorMessage(error)} />;

  return (
    <>
      <div className="page-head">
        <div>
          <h3>Profilim</h3>
          <p className="muted" style={{ fontSize: '0.9rem' }}>
            Hesap bilgilerinizi görüntüleyin ve güncelleyin.
          </p>
        </div>
      </div>

      {/* Ana grid: profil kartı + portföy paneli yan yana (geniş ekran) */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'minmax(0,380px) 1fr',
          gap: 24,
          marginTop: 20,
          alignItems: 'start',
        }}
      >
        {/* Sol: profil kartı */}
        <div className="glass profile-card" style={{ margin: 0, maxWidth: 'none' }}>
          {/* Avatar — ad soyadının ilk harfi */}
          <div className="avatar profile-avatar">{data.fullName[0]}</div>
          <h2>{data.fullName}</h2>
          <p style={{ color: 'var(--accent-gold)', marginBottom: 20 }}>
            {data.position ?? data.roles.join(', ')}
          </p>

          {/* Profil bilgileri */}
          <div className="profile-info">
            <span>
              <strong>E-posta:</strong> {data.email}
            </span>
            <span>
              <strong>Telefon:</strong> {data.phone ?? '-'}
            </span>
            <span>
              <strong>Roller:</strong> {data.roles.join(', ') || '-'}
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

        {/* Sag: portföy paneli */}
        <div className="glass card" style={{ minWidth: 0 }}>
          <div className="card-head">
            <h4>Portföyüm</h4>
            <span
              style={{
                background: 'rgba(227,6,19,0.15)',
                border: '1px solid rgba(227,6,19,0.3)',
                color: 'var(--accent-gold)',
                padding: '3px 10px',
                borderRadius: 'var(--radius-pill)',
                fontSize: '0.78rem',
                fontWeight: 700,
              }}
            >
              {myPortfolio.length} Firma
            </span>
          </div>

          {customersQuery.isError ? (
            <StateBlock message={getErrorMessage(customersQuery.error)} />
          ) : myPortfolio.length === 0 ? (
            <p className="muted" style={{ fontSize: '0.85rem' }}>
              Size atanmış müşteri bulunamadı.
            </p>
          ) : (
            <div className="data-table-container" style={{ marginTop: 0 }}>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Firma Ünvanı</th>
                    <th>Sektör</th>
                    <th>Şehir</th>
                    <th>Durum</th>
                  </tr>
                </thead>
                <tbody>
                  {myPortfolio.map((company) => (
                    <tr
                      key={company.id}
                      className="clickable"
                      onClick={() => navigate(`/companies/${company.id}`)}
                      style={{ cursor: 'pointer' }}
                    >
                      <td>
                        <strong>{company.title}</strong>
                      </td>
                      <td>
                        <span className="badge badge-neutral">
                          {SECTOR_LABELS[company.sector]}
                        </span>
                      </td>
                      <td className="muted" style={{ fontSize: '0.85rem' }}>
                        {company.city ?? '-'}
                      </td>
                      <td>
                        {company.customerStatus ? (
                          <span
                            className={`badge ${company.customerStatus === 'Active' ? 'badge-customer' : 'badge-neutral'}`}
                          >
                            {CUSTOMER_STATUS_LABELS[company.customerStatus]}
                          </span>
                        ) : (
                          '-'
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
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
