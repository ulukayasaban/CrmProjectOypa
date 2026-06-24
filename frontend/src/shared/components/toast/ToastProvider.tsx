/**
 * Global Toast sistemi.
 * Sağ-altta yığılır, otomatik kapanır, manuel kapatılabilir.
 * Erişilebilirlik: role="status" + aria-live="polite"
 */
import {
  createContext,
  useCallback,
  useContext,
  useRef,
  useState,
  type ReactNode,
} from 'react';

// ─── Tipler ─────────────────────────────────────────────────────────────────

export type ToastType = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  type: ToastType;
  message: string;
}

interface ToastContextValue {
  success: (message: string) => void;
  error: (message: string) => void;
  info: (message: string) => void;
}

// ─── Context ─────────────────────────────────────────────────────────────────

const ToastContext = createContext<ToastContextValue | null>(null);

// ─── Provider ────────────────────────────────────────────────────────────────

const AUTO_CLOSE_MS = 3500;

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  // Benzersiz id üretmek için ref (render tetiklemez)
  const idRef = useRef(0);

  /** Toast ekle, AUTO_CLOSE_MS sonra otomatik kaldır. */
  const add = useCallback((type: ToastType, message: string) => {
    const id = ++idRef.current;
    setToasts((prev) => [...prev, { id, type, message }]);
    setTimeout(() => {
      setToasts((prev) => prev.filter((t) => t.id !== id));
    }, AUTO_CLOSE_MS);
  }, []);

  const dismiss = useCallback((id: number) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const ctx: ToastContextValue = {
    success: (msg) => add('success', msg),
    error: (msg) => add('error', msg),
    info: (msg) => add('info', msg),
  };

  return (
    <ToastContext.Provider value={ctx}>
      {children}
      {/* pointer-events:none konteyner → sadece toast'larda auto → sayfa tıklanabilir */}
      <div className="toast-container" aria-live="polite" aria-atomic="false">
        {toasts.map((toast) => (
          <div
            key={toast.id}
            className={`toast toast--${toast.type}`}
            role="status"
          >
            <span className="toast__message">{toast.message}</span>
            <button
              type="button"
              className="toast__close"
              aria-label="Bildirimi kapat"
              onClick={() => dismiss(toast.id)}
            >
              &times;
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

// ─── Hook ────────────────────────────────────────────────────────────────────

/**
 * toast.success / toast.error / toast.info şeklinde kullanılır.
 * ToastProvider dışında çağrılırsa hata fırlatır.
 */
export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext);
  if (!ctx) {
    throw new Error('useToast, ToastProvider içinde kullanılmalıdır.');
  }
  return ctx;
}
