import { useState } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../app/providers/useAuth';
import { useSalesReps } from '../features/salesreps/model/useSalesReps';
import { SalesRepFormModal } from '../features/salesreps/ui/SalesRepFormModal';
import { RegisterUserModal } from '../features/auth/ui/RegisterUserModal';
import { UserRoleModal } from '../features/auth/ui/UserRoleModal';
import { useDeleteUser, useResetUserPassword, useUsers } from '../features/auth/model/useUsers';
import { AccountCredentialDialog } from '../features/employees/ui/AccountCredentialDialog';
import { CategoryManagementSection } from '../features/categories/ui/CategoryManagementSection';
import { TableSkeleton } from '../shared/components/TableSkeleton';
import { StateBlock } from '../shared/components/StateBlock';
import { PlusIcon } from '../shared/components/icons';
import { useToast } from '../shared/components/toast/ToastProvider';
import { useConfirm } from '../shared/hooks/useConfirm';
import { getErrorMessage } from '../shared/lib/errorMessage';
import type { UserDto } from '../entities/user/model/user';
import type { AccountCredentials } from '../features/employees/api/employeeApi';

export default function ManagementPage() {
  const { hasRole, user: currentUser } = useAuth();
  const [salesRepModal, setSalesRepModal] = useState(false);
  const [userModal, setUserModal] = useState(false);
  const [roleModalUser, setRoleModalUser] = useState<UserDto | null>(null);
  const [credentials, setCredentials] = useState<AccountCredentials | null>(null);

  const salesReps = useSalesReps();
  const users = useUsers();
  const deleteUser = useDeleteUser();
  const resetPassword = useResetUserPassword();
  const toast = useToast();
  const { confirm, ConfirmEl } = useConfirm();

  if (!hasRole('Admin')) {
    return <Navigate to="/" replace />;
  }

  /** Kullanıcıyı onay alarak siler. Kendini silme backend tarafından engellenir. */
  async function handleDeleteUser(userId: string, fullName: string) {
    const confirmed = await confirm({
      title: 'Kullanıcıyı Sil',
      message: `"${fullName}" kullanıcısını silmek istiyor musunuz? Bu işlem geri alınamaz.`,
      confirmLabel: 'Sil',
    });
    if (!confirmed) return;

    try {
      await deleteUser.mutateAsync(userId);
      toast.success('Kullanıcı silindi.');
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  }

  /** Parolayı onay alarak sıfırlar; dönen geçici parolayı dialog ile gösterir. */
  async function handleResetPassword(userId: string, fullName: string) {
    const confirmed = await confirm({
      title: 'Parolayı Sıfırla',
      message: `"${fullName}" kullanıcısının parolasını sıfırlamak istiyor musunuz? Geçici bir parola oluşturulacak.`,
      confirmLabel: 'Sıfırla',
    });
    if (!confirmed) return;

    try {
      const result = await resetPassword.mutateAsync(userId);
      setCredentials(result);
      toast.success('Parola sıfırlandı. Geçici parolayı kullanıcıya iletmeyi unutmayın.');
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  }

  return (
    <>
      <div className="page-head">
        <div>
          <h3>Yönetim</h3>
          <p className="muted" style={{ fontSize: '0.9rem' }}>
            Satış temsilcileri, kullanıcı hesapları ve kategori tanımlarını yönetin.
          </p>
        </div>
      </div>

      <div className="dashboard-grid">
        <div className="glass full-width card">
          <div className="card-head">
            <h3>Satış Temsilcileri</h3>
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              onClick={() => setSalesRepModal(true)}
            >
              <PlusIcon size={14} /> Yeni Temsilci
            </button>
          </div>
          {salesReps.isLoading && <TableSkeleton columns={2} rows={4} />}
          {salesReps.isError && (
            <StateBlock message={getErrorMessage(salesReps.error)} />
          )}
          {salesReps.data && (
            <div
              className="data-table-container"
              style={{ background: 'none', border: 'none', marginTop: 15 }}
            >
              <table className="data-table">
                <thead>
                  <tr>
                    <th>İsim</th>
                    <th>E-posta</th>
                  </tr>
                </thead>
                <tbody>
                  {salesReps.data.length === 0 && (
                    <tr>
                      <td colSpan={2} className="table-empty">
                        Henüz temsilci eklenmemiş.
                      </td>
                    </tr>
                  )}
                  {salesReps.data.map((rep) => (
                    <tr key={rep.id}>
                      <td>{rep.name}</td>
                      <td>{rep.email}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="glass full-width card">
          <div className="card-head">
            <h3>Kullanıcı Yönetimi</h3>
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              onClick={() => setUserModal(true)}
            >
              <PlusIcon size={14} /> Yeni Kullanıcı
            </button>
          </div>
          {users.isLoading && <TableSkeleton columns={4} rows={4} />}
          {users.isError && (
            <StateBlock message={getErrorMessage(users.error)} />
          )}
          {users.data && (
            <div
              className="data-table-container"
              style={{ background: 'none', border: 'none', marginTop: 15 }}
            >
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Ad Soyad</th>
                    <th>E-posta</th>
                    <th>Roller</th>
                    <th>İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {users.data.length === 0 && (
                    <tr>
                      <td colSpan={4} className="table-empty">
                        Henüz kullanıcı eklenmemiş.
                      </td>
                    </tr>
                  )}
                  {users.data.map((u) => {
                    const isSelf = u.id === currentUser?.id;
                    return (
                      <tr key={u.id}>
                        <td>{u.fullName}</td>
                        <td>{u.email}</td>
                        <td>{u.roles.join(', ')}</td>
                        <td>
                          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4 }}>
                            {/* Giriş yapan admin kendi rolünü değiştiremez; backend 403 döner, UI'da da gizlenir */}
                            {!isSelf && (
                              <button
                                type="button"
                                className="btn btn-ghost btn-sm"
                                onClick={() => setRoleModalUser(u)}
                              >
                                Rol Değiştir
                              </button>
                            )}
                            <button
                              type="button"
                              className="btn btn-ghost btn-sm"
                              disabled={resetPassword.isPending}
                              onClick={() => void handleResetPassword(u.id, u.fullName)}
                            >
                              Parola Sıfırla
                            </button>
                            {/* Giriş yapan admin kendini silemez (backend de engeller); UI'da da gizle */}
                            {!isSelf && (
                              <button
                                type="button"
                                className="btn btn-ghost btn-sm"
                                disabled={deleteUser.isPending}
                                onClick={() => void handleDeleteUser(u.id, u.fullName)}
                              >
                                Sil
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <CategoryManagementSection />
      </div>

      {ConfirmEl}

      {salesRepModal && (
        <SalesRepFormModal onClose={() => setSalesRepModal(false)} />
      )}
      {userModal && (
        <RegisterUserModal
          onClose={() => {
            setUserModal(false);
          }}
        />
      )}
      {roleModalUser && (
        <UserRoleModal
          user={roleModalUser}
          onClose={() => setRoleModalUser(null)}
        />
      )}
      {credentials && (
        <AccountCredentialDialog
          credentials={credentials}
          onClose={() => setCredentials(null)}
        />
      )}
    </>
  );
}
