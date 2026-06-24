/**
 * NotificationPreferencesModal
 * Kullanıcının hangi bildirim türlerini almak istediğini seçmesini sağlar.
 * Paylaşılan Modal ve useToast (useUpdateNotificationPreferences içinde) kullanır.
 */
import { useState } from 'react';
import { Modal } from '../../../shared/components/Modal';
import { getErrorMessage } from '../../../shared/lib/errorMessage';
import {
  useNotificationPreferences,
  useUpdateNotificationPreferences,
} from '../model/useNotifications';
import type { NotificationPreferenceItem } from '../api/notificationApi';

interface NotificationPreferencesModalProps {
  onClose: () => void;
}

/**
 * Bildirim türlerine kullanıcıya gösterilecek Türkçe etiketler.
 * Manual tipi her zaman açık olduğu için listede yer almaz.
 */
const PREFERENCE_LABELS: Record<string, string> = {
  MeetingScheduled: 'Görüşme Planlandı',
  MeetingNoteAdded: 'Görüşmeye Not Eklendi',
  GoalAssigned: 'Hedef Atandı',
  LeadConverted: 'Müşteriye Dönüştü',
  TenderApproaching: 'Yaklaşan İhale',
};

/** Desteklenen tercih tiplerinin sırası */
const PREFERENCE_TYPES = Object.keys(PREFERENCE_LABELS);

/** Backend listesini tip→enabled eşlemesine çevirir (kayıt yoksa varsayılan açık). */
function buildInitialPrefs(data: NotificationPreferenceItem[]): Record<string, boolean> {
  const merged: Record<string, boolean> = Object.fromEntries(
    PREFERENCE_TYPES.map((t) => [t, true]),
  );
  for (const item of data) {
    if (item.type in merged) merged[item.type] = item.enabled;
  }
  return merged;
}

export function NotificationPreferencesModal({
  onClose,
}: NotificationPreferencesModalProps) {
  const prefsQuery = useNotificationPreferences();

  return (
    <Modal title="Bildirim Tercihleri" onClose={onClose} width={440}>
      <div className="crm-form">
        {prefsQuery.isLoading && (
          <div style={{ display: 'flex', justifyContent: 'center', padding: '24px 0' }}>
            <div className="spinner" aria-label="Yükleniyor" />
          </div>
        )}

        {prefsQuery.isError && (
          <p className="form-error">{getErrorMessage(prefsQuery.error)}</p>
        )}

        {/* Veri yüklendikten sonra formu, ilk değerleri sunucudan alarak render et.
            (Yerel state effect'te değil, mount'ta initialize edilir → cascading render yok.) */}
        {prefsQuery.data && (
          <PreferencesForm
            initial={buildInitialPrefs(prefsQuery.data)}
            onClose={onClose}
          />
        )}
      </div>
    </Modal>
  );
}

interface PreferencesFormProps {
  initial: Record<string, boolean>;
  onClose: () => void;
}

/** Tercih formu — yerel durum mount'ta `initial`'dan başlatılır (effect yok). */
function PreferencesForm({ initial, onClose }: PreferencesFormProps) {
  const updateMutation = useUpdateNotificationPreferences();
  const [localPrefs, setLocalPrefs] = useState<Record<string, boolean>>(initial);

  function handleToggle(type: string) {
    setLocalPrefs((prev) => ({ ...prev, [type]: !prev[type] }));
  }

  function handleSave() {
    const items: NotificationPreferenceItem[] = PREFERENCE_TYPES.map((t) => ({
      type: t,
      enabled: localPrefs[t] ?? true,
    }));
    updateMutation.mutate(items, { onSuccess: onClose });
  }

  const isSaving = updateMutation.isPending;

  return (
    <>
      {updateMutation.isError && (
        <p className="form-error">{getErrorMessage(updateMutation.error)}</p>
      )}

      <fieldset style={{ border: 'none', padding: 0, margin: 0 }} disabled={isSaving}>
        <legend className="muted" style={{ fontSize: '0.8rem', marginBottom: 12 }}>
          Almak istediğiniz bildirim türlerini seçin.
        </legend>

        <ul
          style={{
            listStyle: 'none',
            padding: 0,
            margin: 0,
            display: 'flex',
            flexDirection: 'column',
            gap: 10,
          }}
        >
          {PREFERENCE_TYPES.map((type) => {
            const inputId = `notif-pref-${type}`;
            return (
              <li key={type} style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <input
                  id={inputId}
                  type="checkbox"
                  checked={localPrefs[type] ?? true}
                  onChange={() => handleToggle(type)}
                  style={{ width: 16, height: 16, cursor: 'pointer', flexShrink: 0 }}
                />
                <label htmlFor={inputId} style={{ cursor: 'pointer', fontSize: '0.9rem' }}>
                  {PREFERENCE_LABELS[type]}
                </label>
              </li>
            );
          })}
        </ul>
      </fieldset>

      <div className="modal-footer" style={{ marginTop: 20 }}>
        <button type="button" className="btn btn-ghost" onClick={onClose} disabled={isSaving}>
          İptal
        </button>
        <button
          type="button"
          className="btn btn-primary"
          onClick={handleSave}
          disabled={isSaving}
        >
          {isSaving ? 'Kaydediliyor...' : 'Kaydet'}
        </button>
      </div>
    </>
  );
}
