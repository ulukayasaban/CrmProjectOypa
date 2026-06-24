/**
 * İmperatif onay diyalogu hook'u.
 * const { confirm, ConfirmEl } = useConfirm()
 * await confirm({ ... }) → true/false döner.
 * ConfirmEl'i bileşenin JSX'ine yerleştir.
 */
import { useCallback, useState } from 'react';
import { ConfirmDialog, type ConfirmDialogProps } from '../components/ConfirmDialog';

type ConfirmOptions = Omit<ConfirmDialogProps, 'onConfirm' | 'onCancel'>;

interface UseConfirmReturn {
  /** Kullanıcının seçimini bekleyen Promise<boolean> döndürür. */
  confirm: (options: ConfirmOptions) => Promise<boolean>;
  /** JSX'e eklenecek diyalog elementi (null olabilir). */
  ConfirmEl: React.ReactElement | null;
}

export function useConfirm(): UseConfirmReturn {
  const [state, setState] = useState<{
    options: ConfirmOptions;
    resolve: (value: boolean) => void;
  } | null>(null);

  const confirm = useCallback((options: ConfirmOptions): Promise<boolean> => {
    return new Promise<boolean>((resolve) => {
      setState({ options, resolve });
    });
  }, []);

  const handleConfirm = useCallback(() => {
    state?.resolve(true);
    setState(null);
  }, [state]);

  const handleCancel = useCallback(() => {
    state?.resolve(false);
    setState(null);
  }, [state]);

  const ConfirmEl =
    state !== null
      ? ConfirmDialog({
          ...state.options,
          onConfirm: handleConfirm,
          onCancel: handleCancel,
        })
      : null;

  return { confirm, ConfirmEl };
}
