import type { ReactNode } from 'react';
import { createPortal } from 'react-dom';

interface ModalProps {
  title: string;
  onClose: () => void;
  children: ReactNode;
  width?: number;
}

export function Modal({ title, onClose, children, width = 500 }: ModalProps) {
  // Portal ile body'ye render edilir: aksi halde modal, header gibi
  // backdrop-filter'lı bir atanın içinde kaldığında `position: fixed`
  // overlay viewport yerine o atanın kutusuna hapsolup tam ekranı kaplamıyor.
  return createPortal(
    <div
      className="modal-overlay"
      onClick={onClose}
      role="presentation"
    >
      <div
        className="modal-content glass"
        style={{ width }}
        onClick={(event) => event.stopPropagation()}
        role="dialog"
        aria-modal="true"
        aria-label={title}
      >
        <div className="modal-header">
          <h3>{title}</h3>
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={onClose}
            aria-label="Kapat"
          >
            &times;
          </button>
        </div>
        {children}
      </div>
    </div>,
    document.body,
  );
}
