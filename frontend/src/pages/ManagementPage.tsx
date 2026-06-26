import { useState } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../app/providers/useAuth';
import { useSalesReps } from '../features/salesreps/model/useSalesReps';
import { SalesRepFormModal } from '../features/salesreps/ui/SalesRepFormModal';
import { RegisterUserModal } from '../features/auth/ui/RegisterUserModal';
import { useDeleteUser, useUsers } from '../features/auth/model/useUsers';
import { CategoryManagementSection } from '../features/categories/ui/CategoryManagementSection';
import { TableSkeleton } from '../shared/components/TableSkeleton';
import { StateBlock } from '../shared/components/StateBlock';
import { PlusIcon } from '../shared/components/icons';
import { useToast } from '../shared/components/toast/ToastProvider';
import { useConfirm } from '../shared/hooks/useConfirm';
import { getErrorMessage } from '../shared/lib/errorMessage';

export default function ManagementPage() {
  const { hasRole, user: currentUser } = useAuth();
  const [salesRepModal, setSalesRepModal] = useState(false);
  const [userModal, setUserModal] = useState(false);

  const salesReps = useSalesReps();
  const users = useUsers();
  const deleteUser = useDeleteUser();
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
                  {users.data.map((u) => (
                    <tr key={u.id}>
                      <td>{u.fullName}</td>
                      <td>{u.email}</td>
                      <td>{u.roles.join(', ')}</td>
                      <td>
                        {/* Giriş yapan admin kendini silemez (backend de engeller); UI'da da gizle */}
                        {u.id !== currentUser?.id && (
                          <button
                            type="button"
                            className="btn btn-ghost btn-sm"
                            disabled={deleteUser.isPending}
                            onClick={() =>
                              void handleDeleteUser(u.id, u.fullName)
                            }
                          >
                            Sil
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
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
            // Yeni kullanıcı eklendikten sonra listeyi tazele
            // (RegisterUserModal içindeki onSuccess callback'i kapanır ve
            //  queryClient invalidate useRegisterUser içinde yoksa burada tetikleriz)
          }}
        />
      )}
    </>
  );
}
