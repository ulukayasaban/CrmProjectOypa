import { useState } from 'react';
import { Modal } from '../../../shared/components/Modal';
import { getErrorMessage } from '../../../shared/lib/errorMessage';
import { useAssignManager } from '../model/useEmployees';
import type { EmployeeDto } from '../../../entities/employee/model/employee';

interface AssignManagerModalProps {
  employee: EmployeeDto;
  candidates: EmployeeDto[];
  onClose: () => void;
}

export function AssignManagerModal({ employee, candidates, onClose }: AssignManagerModalProps) {
  const assignManager = useAssignManager();
  // Pre-select current manager if set
  const [selectedManagerId, setSelectedManagerId] = useState<string>(
    employee.managerId ?? '',
  );

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    await assignManager.mutateAsync({
      id: employee.id,
      payload: { managerId: selectedManagerId === '' ? null : selectedManagerId },
    });
    onClose();
  }

  // Exclude the employee themselves from the candidates list
  const filtered = candidates.filter((c) => c.id !== employee.id);

  return (
    <Modal title="Yönetici Ata" onClose={onClose} width={480}>
      <form className="crm-form" onSubmit={(e) => { void handleSubmit(e); }}>
        {assignManager.isError && (
          <div className="form-error">{getErrorMessage(assignManager.error)}</div>
        )}
        <div className="form-group">
          <label htmlFor="managerId">
            <strong>{employee.title}</strong>
            {employee.fullName ? ` — ${employee.fullName}` : ''} için yönetici
          </label>
          <select
            id="managerId"
            value={selectedManagerId}
            onChange={(e) => setSelectedManagerId(e.target.value)}
          >
            <option value="">Havuz / Üst yok</option>
            {filtered.map((c) => (
              <option key={c.id} value={c.id}>
                {c.title}{c.fullName ? ` — ${c.fullName}` : ''}
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
            disabled={assignManager.isPending}
          >
            {assignManager.isPending ? 'Kaydediliyor...' : 'Kaydet'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
