import { useState } from 'react';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { PlusIcon } from '../shared/components/icons';
import { getErrorMessage } from '../shared/lib/errorMessage';
import { USER_ROLE_LABELS } from '../shared/constants/labels';
import {
  useManagedEmployees,
  useRemoveEmployee,
  useCreateEmployeeAccount,
  useResetEmployeePassword,
  useUnlinkEmployeeAccount,
} from '../features/employees/model/useEmployees';
import { EmployeeFormModal } from '../features/employees/ui/EmployeeFormModal';
import { AssignManagerModal } from '../features/employees/ui/AssignManagerModal';
import { AssignRoleModal } from '../features/employees/ui/AssignRoleModal';
import { AccountCredentialDialog } from '../features/employees/ui/AccountCredentialDialog';
import type { EmployeeDto } from '../entities/employee/model/employee';
import type { AccountCredentials } from '../features/employees/api/employeeApi';
import type { UserRole } from '../shared/types/enums';

type ActiveModal =
  | { type: 'create' }
  | { type: 'edit'; employee: EmployeeDto }
  | { type: 'assign-manager'; employee: EmployeeDto }
  | { type: 'assign-role'; employee: EmployeeDto }
  | { type: 'create-account'; employee: EmployeeDto };

export default function EmployeeManagementPage() {
  const managed = useManagedEmployees();
  const removeEmployee = useRemoveEmployee();
  const createAccount = useCreateEmployeeAccount();
  const resetPassword = useResetEmployeePassword();
  const unlinkAccount = useUnlinkEmployeeAccount();

  const [activeModal, setActiveModal] = useState<ActiveModal | null>(null);
  const [credentials, setCredentials] = useState<AccountCredentials | null>(null);
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null);

  function closeModal() {
    setActiveModal(null);
  }

  async function handleDelete(id: string) {
    if (!window.confirm('Bu personeli silmek istediğinize emin misiniz?')) return;
    await removeEmployee.mutateAsync(id);
    if (pendingDeleteId === id) setPendingDeleteId(null);
  }

  async function handleCreateAccount(employee: EmployeeDto) {
    setActiveModal({ type: 'create-account', employee });
  }

  async function handleResetPassword(employee: EmployeeDto) {
    if (!window.confirm(`${employee.title} için parola sıfırlansın mı?`)) return;
    const result = await resetPassword.mutateAsync(employee.id);
    setCredentials(result);
  }

  async function handleUnlinkAccount(employee: EmployeeDto) {
    if (!window.confirm(`${employee.title} için hesap bağlantısı kaldırılsın mı?`)) return;
    await unlinkAccount.mutateAsync(employee.id);
  }

  // CreateAccount action: show role select then call API
  async function handleCreateAccountSubmit(employee: EmployeeDto, role: string) {
    const result = await createAccount.mutateAsync({ id: employee.id, payload: { role } });
    setCredentials(result);
    closeModal();
  }

  const employeeList = managed.data ?? [];

  return (
    <>
      <div className="glass full-width card">
        <div className="card-head">
          <h3>Personel Listesi</h3>
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={() => setActiveModal({ type: 'create' })}
          >
            <PlusIcon size={14} /> Yeni Personel
          </button>
        </div>

        {managed.isLoading && <Spinner />}
        {managed.isError && <StateBlock message={getErrorMessage(managed.error)} />}

        {managed.data && (
          <div
            className="data-table-container"
            style={{ background: 'none', border: 'none', marginTop: 15 }}
          >
            <table className="data-table">
              <thead>
                <tr>
                  <th>Ünvan</th>
                  <th>Ad Soyad</th>
                  <th>E-posta</th>
                  <th>Yönetici</th>
                  <th>Hesap / Rol</th>
                  <th>İşlemler</th>
                </tr>
              </thead>
              <tbody>
                {employeeList.length === 0 && (
                  <tr>
                    <td colSpan={6} className="table-empty">
                      Kapsamınızda personel bulunmuyor.
                    </td>
                  </tr>
                )}
                {employeeList.map((emp) => (
                  <tr key={emp.id}>
                    <td>{emp.title}</td>
                    <td>{emp.fullName ?? <span className="muted">—</span>}</td>
                    <td>{emp.email ?? <span className="muted">—</span>}</td>
                    <td>{emp.managerName ?? <span className="muted">—</span>}</td>
                    <td>
                      {emp.hasAccount && emp.role ? (
                        <span
                          className="badge"
                          style={{
                            background: emp.role === 'Admin' ? 'rgba(255,180,0,0.18)' : 'rgba(0,200,150,0.18)',
                            color: emp.role === 'Admin' ? 'var(--accent-gold)' : '#00c896',
                            borderRadius: 6,
                            padding: '2px 8px',
                            fontSize: '0.78rem',
                          }}
                        >
                          {USER_ROLE_LABELS[emp.role as UserRole] ?? emp.role}
                        </span>
                      ) : (
                        <span className="muted" style={{ fontSize: '0.8rem' }}>Hesap yok</span>
                      )}
                    </td>
                    <td>
                      <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
                        <button
                          type="button"
                          className="btn btn-ghost btn-sm"
                          onClick={() => setActiveModal({ type: 'edit', employee: emp })}
                        >
                          Düzenle
                        </button>
                        <button
                          type="button"
                          className="btn btn-ghost btn-sm"
                          onClick={() => setActiveModal({ type: 'assign-manager', employee: emp })}
                        >
                          Yönetici Ata
                        </button>
                        {!emp.hasAccount && (
                          <button
                            type="button"
                            className="btn btn-ghost btn-sm"
                            onClick={() => void handleCreateAccount(emp)}
                          >
                            Hesap Oluştur
                          </button>
                        )}
                        {emp.hasAccount && (
                          <>
                            <button
                              type="button"
                              className="btn btn-ghost btn-sm"
                              onClick={() => setActiveModal({ type: 'assign-role', employee: emp })}
                            >
                              Rol Ata
                            </button>
                            <button
                              type="button"
                              className="btn btn-ghost btn-sm"
                              disabled={resetPassword.isPending}
                              onClick={() => void handleResetPassword(emp)}
                            >
                              Parola Sıfırla
                            </button>
                            <button
                              type="button"
                              className="btn btn-ghost btn-sm"
                              disabled={unlinkAccount.isPending}
                              onClick={() => void handleUnlinkAccount(emp)}
                            >
                              Hesabı Ayır
                            </button>
                          </>
                        )}
                        <button
                          type="button"
                          className="btn btn-ghost btn-sm"
                          style={{ color: '#e05555' }}
                          disabled={removeEmployee.isPending && pendingDeleteId === emp.id}
                          onClick={() => {
                            setPendingDeleteId(emp.id);
                            void handleDelete(emp.id);
                          }}
                        >
                          Sil
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {activeModal?.type === 'create' && (
        <EmployeeFormModal
          managedList={employeeList}
          onClose={closeModal}
          onCredentials={(creds) => setCredentials(creds)}
        />
      )}

      {activeModal?.type === 'edit' && (
        <EmployeeFormModal
          employee={activeModal.employee}
          managedList={employeeList}
          onClose={closeModal}
        />
      )}

      {activeModal?.type === 'assign-manager' && (
        <AssignManagerModal
          employee={activeModal.employee}
          candidates={employeeList}
          onClose={closeModal}
        />
      )}

      {activeModal?.type === 'assign-role' && (
        <AssignRoleModal
          employee={activeModal.employee}
          onClose={closeModal}
        />
      )}

      {activeModal?.type === 'create-account' && (
        <CreateAccountModal
          employee={activeModal.employee}
          isPending={createAccount.isPending}
          isError={createAccount.isError}
          error={createAccount.error}
          onSubmit={handleCreateAccountSubmit}
          onClose={closeModal}
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

// Inline lightweight modal for creating an account for an existing employee
interface CreateAccountModalProps {
  employee: EmployeeDto;
  isPending: boolean;
  isError: boolean;
  error: unknown;
  onSubmit: (employee: EmployeeDto, role: string) => void;
  onClose: () => void;
}

import { Modal } from '../shared/components/Modal';
import { USER_ROLE_OPTIONS } from '../shared/constants/labels';

function CreateAccountModal({
  employee,
  isPending,
  isError,
  error,
  onSubmit,
  onClose,
}: CreateAccountModalProps) {
  const [role, setRole] = useState('');

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!role) return;
    onSubmit(employee, role);
  }

  return (
    <Modal title="Hesap Oluştur" onClose={onClose} width={400}>
      <form className="crm-form" onSubmit={(e) => { void handleSubmit(e); }}>
        {isError && (
          <div className="form-error">{getErrorMessage(error)}</div>
        )}
        <p className="muted" style={{ fontSize: '0.85rem' }}>
          <strong>{employee.title}{employee.fullName ? ` — ${employee.fullName}` : ''}</strong>{' '}
          için sistem hesabı oluşturulacak.
          {employee.email && (
            <> E-posta: <strong>{employee.email}</strong></>
          )}
        </p>
        <div className="form-group">
          <label htmlFor="account-role">Rol</label>
          <select
            id="account-role"
            value={role}
            onChange={(e) => setRole(e.target.value)}
            required
          >
            <option value="" disabled>Seçiniz</option>
            {USER_ROLE_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
        <div className="modal-footer">
          <button type="button" className="btn btn-ghost" onClick={onClose}>
            İptal
          </button>
          <button type="submit" className="btn btn-primary" disabled={isPending || !role}>
            {isPending ? 'Oluşturuluyor...' : 'Oluştur'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
