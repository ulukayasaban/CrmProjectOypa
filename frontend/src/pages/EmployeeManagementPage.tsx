import React, { useState } from 'react';
import { Modal } from '../shared/components/Modal';
import { RowActionsMenu, type RowAction } from '../shared/components/RowActionsMenu';
import { TableSkeleton } from '../shared/components/TableSkeleton';
import { StateBlock } from '../shared/components/StateBlock';
import { Pagination } from '../shared/components/Pagination';
import { SortableTh } from '../shared/components/SortableTh';
import { PlusIcon } from '../shared/components/icons';
import { useToast } from '../shared/components/toast/ToastProvider';
import { useConfirm } from '../shared/hooks/useConfirm';
import { useDebouncedValue } from '../shared/hooks/useDebouncedValue';
import { getErrorMessage } from '../shared/lib/errorMessage';
import { USER_ROLE_LABELS, USER_ROLE_OPTIONS } from '../shared/constants/labels';
import {
  // Tam-liste hook'lar: modal/operasyon için kullanılmaya devam eder
  useManagedEmployees,
  useRemoveEmployee,
  useCreateEmployeeAccount,
  useResetEmployeePassword,
  useUnlinkEmployeeAccount,
  // Yeni: yalnızca tablo için sayfalı hook
  useEmployeesManagedPaged,
} from '../features/employees/model/useEmployees';
import { EmployeeFormModal } from '../features/employees/ui/EmployeeFormModal';
import { AssignManagerModal } from '../features/employees/ui/AssignManagerModal';
import { AssignRoleModal } from '../features/employees/ui/AssignRoleModal';
import { AccountCredentialDialog } from '../features/employees/ui/AccountCredentialDialog';
import type { EmployeeDto } from '../entities/employee/model/employee';
import type { AccountCredentials } from '../features/employees/api/employeeApi';
import type { UserRole } from '../shared/types/enums';

/** Personel tablosu sıralanabilir sütunları. */
type EmployeeSortField = 'fullName' | 'title' | 'email';

const DEFAULT_SORT_BY: EmployeeSortField = 'fullName';
const DEFAULT_SORT_DIR = 'asc' as const;
const DEFAULT_PAGE_SIZE = 20;

type ActiveModal =
  | { type: 'create' }
  | { type: 'edit'; employee: EmployeeDto }
  | { type: 'assign-manager'; employee: EmployeeDto }
  | { type: 'assign-role'; employee: EmployeeDto }
  | { type: 'create-account'; employee: EmployeeDto };

export default function EmployeeManagementPage() {
  // Tam-liste: modal ve operasyonlar için (Sidebar/NotificationBell de bu key'i kullanır)
  const managed = useManagedEmployees();
  const toast = useToast();
  const { confirm, ConfirmEl } = useConfirm();

  // Tablo için sayfalı sorgu
  const [searchInput, setSearchInput] = useState('');
  const [sortBy, setSortBy] = useState<EmployeeSortField>(DEFAULT_SORT_BY);
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>(DEFAULT_SORT_DIR);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);

  // 300ms gecikme ile arama
  const search = useDebouncedValue(searchInput, 300);

  const pagedQuery = useEmployeesManagedPaged({
    search: search || undefined,
    sortBy,
    sortDir,
    page,
    pageSize,
  });

  const removeEmployee = useRemoveEmployee();
  const createAccount = useCreateEmployeeAccount();
  const resetPassword = useResetEmployeePassword();
  const unlinkAccount = useUnlinkEmployeeAccount();

  const [activeModal, setActiveModal] = useState<ActiveModal | null>(null);
  const [credentials, setCredentials] = useState<AccountCredentials | null>(null);
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null);

  // Modal işlemleri için tam liste — hata durumunda boş dizi
  const fullEmployeeList = managed.data ?? [];

  function closeModal() {
    setActiveModal(null);
  }

  async function handleDelete(id: string) {
    const confirmed = await confirm({
      title: 'Personeli Sil',
      message: 'Bu personeli silmek istediğinize emin misiniz?',
      confirmLabel: 'Sil',
      danger: true,
    });
    if (!confirmed) return;

    try {
      await removeEmployee.mutateAsync(id);
      toast.success('Personel silindi.');
      if (pendingDeleteId === id) setPendingDeleteId(null);
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  }

  async function handleCreateAccount(employee: EmployeeDto) {
    setActiveModal({ type: 'create-account', employee });
  }

  async function handleResetPassword(employee: EmployeeDto) {
    const confirmed = await confirm({
      title: 'Parola Sıfırla',
      message: `${employee.title} için parola sıfırlansın mı?`,
      confirmLabel: 'Sıfırla',
    });
    if (!confirmed) return;

    try {
      const result = await resetPassword.mutateAsync(employee.id);
      toast.success('Parola sıfırlandı.');
      setCredentials(result);
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  }

  async function handleUnlinkAccount(employee: EmployeeDto) {
    const confirmed = await confirm({
      title: 'Hesap Bağlantısını Kaldır',
      message: `${employee.title} için hesap bağlantısı kaldırılsın mı?`,
      confirmLabel: 'Kaldır',
      danger: true,
    });
    if (!confirmed) return;

    try {
      await unlinkAccount.mutateAsync(employee.id);
      toast.success('Hesap bağlantısı kaldırıldı.');
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  }

  async function handleCreateAccountSubmit(employee: EmployeeDto, role: string) {
    const result = await createAccount.mutateAsync({ id: employee.id, payload: { role } });
    toast.success('Hesap oluşturuldu.');
    setCredentials(result);
    closeModal();
  }

  /** Sıralama değişince sayfayı başa al. */
  function handleSort(field: string, dir: 'asc' | 'desc') {
    setSortBy(field as EmployeeSortField);
    setSortDir(dir);
    setPage(1);
  }

  /** Arama değişince sayfayı başa al. */
  function handleSearchChange(value: string) {
    setSearchInput(value);
    setPage(1);
  }

  /** Sayfa boyutu değişince sayfayı başa al. */
  function handlePageSizeChange(size: number) {
    setPageSize(size);
    setPage(1);
  }

  /** Satır aksiyon menüsü öğelerini personelin hesap durumuna göre kurar. */
  function buildRowActions(emp: EmployeeDto): RowAction[] {
    const actions: RowAction[] = [
      {
        label: 'Yönetici Ata',
        onClick: () => setActiveModal({ type: 'assign-manager', employee: emp }),
      },
    ];

    if (!emp.hasAccount) {
      actions.push({
        label: 'Hesap Oluştur',
        onClick: () => void handleCreateAccount(emp),
      });
    } else {
      actions.push(
        {
          label: 'Rol Ata',
          onClick: () => setActiveModal({ type: 'assign-role', employee: emp }),
        },
        {
          label: 'Parola Sıfırla',
          onClick: () => void handleResetPassword(emp),
          disabled: resetPassword.isPending,
        },
        {
          label: 'Hesabı Ayır',
          onClick: () => void handleUnlinkAccount(emp),
          disabled: unlinkAccount.isPending,
        },
      );
    }

    actions.push({
      label: 'Sil',
      danger: true,
      disabled: removeEmployee.isPending && pendingDeleteId === emp.id,
      onClick: () => {
        setPendingDeleteId(emp.id);
        void handleDelete(emp.id);
      },
    });

    return actions;
  }

  const items = pagedQuery.data?.items ?? [];
  const totalPages = pagedQuery.data?.totalPages ?? 1;
  const totalCount = pagedQuery.data?.totalCount ?? 0;

  return (
    <>
      <div className="page-head">
        <div>
          <h3>Personel Listesi</h3>
          <p className="muted" style={{ fontSize: '0.9rem' }}>
            Kapsamınızdaki personeli, hesaplarını ve rollerini yönetin.
          </p>
        </div>
        <button
          type="button"
          className="btn btn-primary"
          onClick={() => setActiveModal({ type: 'create' })}
        >
          <PlusIcon size={14} /> Yeni Personel
        </button>
      </div>

      {/* Arama kutusu */}
      <div style={{ marginBottom: 12 }}>
        <input
          type="search"
          placeholder="Ad, ünvan veya e-posta ara..."
          aria-label="Personel ara"
          value={searchInput}
          onChange={(event) => handleSearchChange(event.target.value)}
          style={{ maxWidth: 320 }}
        />
      </div>

      {pagedQuery.isLoading && <TableSkeleton columns={6} />}
      {pagedQuery.isError && (
        <StateBlock message={getErrorMessage(pagedQuery.error)} />
      )}

      {!pagedQuery.isLoading && !pagedQuery.isError && (
        <>
          <div className="data-table-container glass">
            <table className="data-table">
                <thead>
                  <tr>
                    <SortableTh
                      field="title"
                      activeSortBy={sortBy}
                      activeSortDir={sortDir}
                      onSort={handleSort}
                    >
                      Ünvan
                    </SortableTh>
                    <SortableTh
                      field="fullName"
                      activeSortBy={sortBy}
                      activeSortDir={sortDir}
                      onSort={handleSort}
                    >
                      Ad Soyad
                    </SortableTh>
                    <SortableTh
                      field="email"
                      activeSortBy={sortBy}
                      activeSortDir={sortDir}
                      onSort={handleSort}
                    >
                      E-posta
                    </SortableTh>
                    <th>Yönetici</th>
                    <th>Hesap / Rol</th>
                    <th>İşlemler</th>
                  </tr>
                </thead>
                <tbody>
                  {items.length === 0 && (
                    <tr>
                      <td colSpan={6} className="table-empty">
                        {search
                          ? `"${search}" için sonuç bulunamadı.`
                          : 'Kapsamınızda personel bulunmuyor.'}
                      </td>
                    </tr>
                  )}
                  {items.map((emp) => (
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
                              background:
                                emp.role === 'Admin'
                                  ? 'rgba(255,180,0,0.18)'
                                  : 'rgba(0,200,150,0.18)',
                              color:
                                emp.role === 'Admin'
                                  ? 'var(--accent-gold)'
                                  : '#00c896',
                              borderRadius: 6,
                              padding: '2px 8px',
                              fontSize: '0.78rem',
                            }}
                          >
                            {USER_ROLE_LABELS[emp.role as UserRole] ?? emp.role}
                          </span>
                        ) : (
                          <span className="muted" style={{ fontSize: '0.8rem' }}>
                            Hesap yok
                          </span>
                        )}
                      </td>
                      <td>
                        <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                          <button
                            type="button"
                            className="btn btn-ghost btn-sm"
                            onClick={() =>
                              setActiveModal({ type: 'edit', employee: emp })
                            }
                          >
                            Düzenle
                          </button>
                          <RowActionsMenu actions={buildRowActions(emp)} />
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <Pagination
              page={page}
              totalPages={totalPages}
              totalCount={totalCount}
              pageSize={pageSize}
              onPageChange={setPage}
              onPageSizeChange={handlePageSizeChange}
            />
          </>
        )}

      {ConfirmEl}

      {activeModal?.type === 'create' && (
        <EmployeeFormModal
          managedList={fullEmployeeList}
          onClose={closeModal}
          onCredentials={(creds) => setCredentials(creds)}
        />
      )}

      {activeModal?.type === 'edit' && (
        <EmployeeFormModal
          employee={activeModal.employee}
          managedList={fullEmployeeList}
          onClose={closeModal}
        />
      )}

      {activeModal?.type === 'assign-manager' && (
        <AssignManagerModal
          employee={activeModal.employee}
          candidates={fullEmployeeList}
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

// ─── Yerel CreateAccountModal ─────────────────────────────────────────────────

interface CreateAccountModalProps {
  employee: EmployeeDto;
  isPending: boolean;
  isError: boolean;
  error: unknown;
  onSubmit: (employee: EmployeeDto, role: string) => void;
  onClose: () => void;
}

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
          <strong>
            {employee.title}
            {employee.fullName ? ` — ${employee.fullName}` : ''}
          </strong>{' '}
          için sistem hesabı oluşturulacak.
          {employee.email && (
            <>
              {' '}
              E-posta: <strong>{employee.email}</strong>
            </>
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
            <option value="" disabled>
              Seçiniz
            </option>
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
          <button
            type="submit"
            className="btn btn-primary"
            disabled={isPending || !role}
          >
            {isPending ? 'Oluşturuluyor...' : 'Oluştur'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
