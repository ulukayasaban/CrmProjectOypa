import { useState } from 'react';
import {
  useMailDrafts,
  useSendMailDraft,
} from '../features/maildrafts/model/useMailDrafts';
import { mailDraftApi } from '../features/maildrafts/api/mailDraftApi';
import { Modal } from '../shared/components/Modal';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { formatDateTime } from '../shared/lib/format';
import { getErrorMessage } from '../shared/lib/errorMessage';
import { saveEml } from '../shared/lib/outlook';
import type { MailDraftDto } from '../entities/maildraft/model/mailDraft';

export default function MailDraftsPage() {
  const { data, isLoading, isError, error } = useMailDrafts();
  const sendDraft = useSendMailDraft();
  const [preview, setPreview] = useState<MailDraftDto | null>(null);
  const [emlPending, setEmlPending] = useState<string | null>(null);

  async function handleDownloadEml(draft: MailDraftDto) {
    setEmlPending(draft.id);
    try {
      const blob = await mailDraftApi.downloadEml(draft.id);
      saveEml(blob, `toplanti-${draft.id}.eml`);
    } finally {
      setEmlPending(null);
    }
  }

  if (isLoading) return <Spinner />;
  if (isError || !data) return <StateBlock message={getErrorMessage(error)} />;

  return (
    <>
      <div className="page-head">
        <div>
          <h3>Mail Simülasyon Merkezi</h3>
          <p className="muted" style={{ fontSize: '0.9rem' }}>
            Sistem tarafından oluşturulan hatırlatma maillerini buradan
            yönetebilirsiniz.
          </p>
        </div>
      </div>

      <div className="data-table-container glass">
        <table className="data-table">
          <thead>
            <tr>
              <th>Oluşturulma</th>
              <th>Alıcı</th>
              <th>Konu</th>
              <th>Durum</th>
              <th>İşlem</th>
            </tr>
          </thead>
          <tbody>
            {data.length === 0 && (
              <tr>
                <td colSpan={5} className="table-empty">
                  Henüz taslak oluşturulmamış.
                </td>
              </tr>
            )}
            {data.map((draft) => (
              <tr key={draft.id}>
                <td style={{ fontSize: '0.8rem' }}>
                  {formatDateTime(draft.createdAtUtc)}
                </td>
                <td>
                  <strong>{draft.to}</strong>
                </td>
                <td style={{ fontSize: '0.85rem' }}>{draft.subject}</td>
                <td>
                  {draft.sent ? (
                    <span className="badge badge-customer">Gönderildi</span>
                  ) : (
                    <span className="badge badge-lead">Beklemede</span>
                  )}
                </td>
                <td>
                  <div className="row-actions">
                    <button
                      type="button"
                      className="btn btn-ghost btn-sm"
                      onClick={() => setPreview(draft)}
                    >
                      Görüntüle
                    </button>
                    {!draft.sent && (
                      <button
                        type="button"
                        className="btn btn-primary btn-sm"
                        disabled={sendDraft.isPending}
                        onClick={() => sendDraft.mutate(draft.id)}
                      >
                        Simüle Gönder
                      </button>
                    )}
                    <button
                      type="button"
                      className="btn btn-ghost btn-sm"
                      disabled={emlPending === draft.id}
                      onClick={() => void handleDownloadEml(draft)}
                      title="E-postayı .eml olarak indir; açıldığında Outlook'ta taslak olarak görüntülenir"
                    >
                      {emlPending === draft.id ? '...' : "Outlook'ta Aç"}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {preview && (
        <Modal
          title={preview.subject}
          onClose={() => setPreview(null)}
          width={600}
        >
          <div className="crm-form">
            <div className="form-group">
              <label>Alıcı</label>
              <div>{preview.to}</div>
            </div>
            {preview.cc && (
              <div className="form-group">
                <label>CC</label>
                <div>{preview.cc}</div>
              </div>
            )}
            <div className="form-group">
              <label>İçerik</label>
              <div className="mail-body">{preview.body}</div>
            </div>
            <div className="modal-footer">
              <button
                type="button"
                className="btn btn-ghost"
                onClick={() => setPreview(null)}
              >
                Kapat
              </button>
            </div>
          </div>
        </Modal>
      )}
    </>
  );
}
