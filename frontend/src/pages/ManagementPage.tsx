import { useState } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../app/providers/useAuth';
import { useSalesReps } from '../features/salesreps/model/useSalesReps';
import { SalesRepFormModal } from '../features/salesreps/ui/SalesRepFormModal';
import { RegisterUserModal } from '../features/auth/ui/RegisterUserModal';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { PlusIcon } from '../shared/components/icons';
import { getErrorMessage } from '../shared/lib/errorMessage';

export default function ManagementPage() {
  const { hasRole } = useAuth();
  const [salesRepModal, setSalesRepModal] = useState(false);
  const [userModal, setUserModal] = useState(false);

  const salesReps = useSalesReps();

  if (!hasRole('Admin')) {
    return <Navigate to="/" replace />;
  }

  return (
    <>
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
          {salesReps.isLoading && <Spinner />}
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
          <p className="muted" style={{ marginTop: 10, fontSize: '0.85rem' }}>
            Sisteme yönetici veya satış kullanıcısı ekleyin.
          </p>
        </div>
      </div>

      {salesRepModal && (
        <SalesRepFormModal onClose={() => setSalesRepModal(false)} />
      )}
      {userModal && <RegisterUserModal onClose={() => setUserModal(false)} />}
    </>
  );
}
