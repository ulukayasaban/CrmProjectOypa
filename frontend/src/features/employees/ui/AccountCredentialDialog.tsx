import { useState } from 'react';
import { Modal } from '../../../shared/components/Modal';
import type { AccountCredentials } from '../api/employeeApi';

interface AccountCredentialDialogProps {
  credentials: AccountCredentials;
  onClose: () => void;
}

export function AccountCredentialDialog({ credentials, onClose }: AccountCredentialDialogProps) {
  const [copied, setCopied] = useState(false);

  function handleCopy() {
    void navigator.clipboard
      .writeText(`E-posta: ${credentials.email}\nGeçici Parola: ${credentials.tempPassword}`)
      .then(() => {
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
      });
  }

  return (
    <Modal title="Hesap Bilgileri" onClose={onClose} width={480}>
      <div className="crm-form">
        <p
          className="form-error"
          style={{ background: 'rgba(255,180,0,0.12)', color: 'var(--accent-gold)', border: '1px solid var(--accent-gold)' }}
        >
          Bu bilgiler yalnızca bir kez gösterilir. Kullanıcıya iletmeyi unutmayın.
        </p>
        <div className="form-group">
          <label>E-posta</label>
          <input type="text" readOnly value={credentials.email} />
        </div>
        <div className="form-group">
          <label>Geçici Parola</label>
          <input type="text" readOnly value={credentials.tempPassword} />
        </div>
        <div className="modal-footer">
          <button type="button" className="btn btn-ghost" onClick={handleCopy}>
            {copied ? 'Kopyalandı!' : 'Kopyala'}
          </button>
          <button type="button" className="btn btn-primary" onClick={onClose}>
            Tamam
          </button>
        </div>
      </div>
    </Modal>
  );
}
