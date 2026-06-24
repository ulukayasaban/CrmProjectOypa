/**
 * window.confirm yerine kullanılan erişilebilir onay diyalogu.
 * Modal (portal) tabanlı; tehlikeli aksiyonlar için kırmızı buton varyantı.
 * useConfirm() hook'u ile imperatif (Promise<boolean>) kullanım desteklenir.
 */
import { createPortal } from 'react-dom';

export interface ConfirmDialogProps {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  /** true → onay butonu kırmızı (silme gibi tehlikeli aksiyonlar için) */
  danger?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function ConfirmDialog({
  title,
  message,
  confirmLabel = 'Onayla',
  cancelLabel = 'Vazgeç',
  danger = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  return createPortal(
    <div
      className="modal-overlay"
      onClick={onCancel}
      role="presentation"
    >
      <div
        className="modal-content glass"
        style={{ width: 420 }}
        onClick={(e) => e.stopPropagation()}
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="confirm-title"
        aria-describedby="confirm-message"
      >
        <div className="modal-header">
          <h3 id="confirm-title">{title}</h3>
        </div>
        <p
          id="confirm-message"
          style={{ fontSize: '0.9rem', color: 'var(--text-muted)', marginBottom: 8 }}
        >
          {message}
        </p>
        <div className="modal-footer">
          <button
            type="button"
            className="btn btn-ghost"
            onClick={onCancel}
            autoFocus
          >
            {cancelLabel}
          </button>
          <button
            type="button"
            className="btn btn-primary"
            style={danger ? { background: 'var(--error)', color: 'white' } : undefined}
            onClick={onConfirm}
          >
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
