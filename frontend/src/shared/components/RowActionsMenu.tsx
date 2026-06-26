import { useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';

export interface RowAction {
  label: string;
  onClick: () => void;
  /** Kırmızı (silme gibi tehlikeli aksiyon). */
  danger?: boolean;
  disabled?: boolean;
}

interface RowActionsMenuProps {
  actions: RowAction[];
  /** Tetikleyici buton etiketi. */
  label?: string;
}

const MENU_WIDTH = 200;

/**
 * Tablo satırlarında çok sayıda aksiyonu kompakt bir açılır menüde toplar.
 * Menü body'ye portal + position:fixed ile render edilir; böylece
 * .data-table-container'ın overflow'u tarafından kırpılmaz. Dışarı tıklama,
 * Escape ve sayfa kaydırma menüyü kapatır.
 */
export function RowActionsMenu({ actions, label = 'İşlemler' }: RowActionsMenuProps) {
  const [open, setOpen] = useState(false);
  const [pos, setPos] = useState<{ top: number; left: number } | null>(null);
  const btnRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;

    function onDocPointer(event: MouseEvent) {
      if (
        menuRef.current?.contains(event.target as Node) ||
        btnRef.current?.contains(event.target as Node)
      ) {
        return;
      }
      setOpen(false);
    }
    function onKey(event: KeyboardEvent) {
      if (event.key === 'Escape') setOpen(false);
    }
    // Kaydırma/yeniden boyutlandırmada konum bozulmasın diye menüyü kapat.
    function onReflow() {
      setOpen(false);
    }

    document.addEventListener('mousedown', onDocPointer);
    document.addEventListener('keydown', onKey);
    window.addEventListener('scroll', onReflow, true);
    window.addEventListener('resize', onReflow);
    return () => {
      document.removeEventListener('mousedown', onDocPointer);
      document.removeEventListener('keydown', onKey);
      window.removeEventListener('scroll', onReflow, true);
      window.removeEventListener('resize', onReflow);
    };
  }, [open]);

  function toggle() {
    if (!open && btnRef.current) {
      const rect = btnRef.current.getBoundingClientRect();
      // Menüyü butonun altına; sağ kenarı butonla hizalı, viewport içinde tut.
      const left = Math.max(
        8,
        Math.min(rect.right - MENU_WIDTH, window.innerWidth - MENU_WIDTH - 8),
      );
      setPos({ top: rect.bottom + 4, left });
    }
    setOpen((value) => !value);
  }

  return (
    <>
      <button
        ref={btnRef}
        type="button"
        className="btn btn-ghost btn-sm"
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={toggle}
      >
        {label} ▾
      </button>
      {open &&
        pos &&
        createPortal(
          <div
            ref={menuRef}
            className="row-actions-menu glass"
            role="menu"
            style={{ position: 'fixed', top: pos.top, left: pos.left, width: MENU_WIDTH }}
          >
            {actions.map((action) => (
              <button
                key={action.label}
                type="button"
                role="menuitem"
                className="row-actions-menu__item"
                disabled={action.disabled}
                style={action.danger ? { color: 'var(--error)' } : undefined}
                onClick={() => {
                  setOpen(false);
                  action.onClick();
                }}
              >
                {action.label}
              </button>
            ))}
          </div>,
          document.body,
        )}
    </>
  );
}
