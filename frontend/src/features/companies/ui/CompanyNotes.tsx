import { useRef, useState } from 'react';
import { useCompanyNotes, useAddCompanyNote } from '../model/useCompanyNotes';
import { Spinner } from '../../../shared/components/Spinner';
import { StateBlock } from '../../../shared/components/StateBlock';
import { formatDateTime } from '../../../shared/lib/format';
import { getErrorMessage } from '../../../shared/lib/errorMessage';

interface CompanyNotesProps {
  companyId: string;
}

/**
 * "Notlar & Bekleyen İşler" bölümü. Firma detay sayfasında kullanılır.
 * MeetingNotes bileşeninin görünüm/markup desenini takip eder.
 */
export function CompanyNotes({ companyId }: CompanyNotesProps) {
  const [content, setContent] = useState('');
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const { data: notes, isLoading, isError, error } = useCompanyNotes(companyId);
  const addNote = useAddCompanyNote(companyId);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const trimmed = content.trim();
    if (!trimmed) return;
    await addNote.mutateAsync(trimmed);
    setContent('');
    textareaRef.current?.focus();
  }

  return (
    <div className="meeting-notes">
      <form className="meeting-notes__form" onSubmit={(e) => void handleSubmit(e)}>
        {addNote.isError && (
          <div className="form-error" style={{ fontSize: '0.8rem', marginBottom: 4 }}>
            {getErrorMessage(addNote.error)}
          </div>
        )}
        <div style={{ display: 'flex', gap: 6, alignItems: 'flex-start' }}>
          <div style={{ flex: 1 }}>
            <textarea
              ref={textareaRef}
              id="company-note-content"
              rows={2}
              placeholder="Not veya bekleyen iş ekle..."
              style={{ width: '100%', resize: 'vertical', fontSize: '0.8rem' }}
              aria-label="Firma notu"
              aria-describedby={addNote.isError ? 'company-note-error' : undefined}
              value={content}
              onChange={(e) => setContent(e.target.value)}
            />
            {addNote.isError && (
              <span id="company-note-error" style={{ display: 'none' }}>
                {getErrorMessage(addNote.error)}
              </span>
            )}
          </div>
          <button
            type="submit"
            className="btn btn-ghost btn-sm"
            disabled={content.trim().length === 0 || addNote.isPending}
            style={{ whiteSpace: 'nowrap', marginTop: 2 }}
          >
            {addNote.isPending ? 'Kaydediliyor...' : 'Notu Kaydet'}
          </button>
        </div>
      </form>

      {isLoading && <Spinner />}
      {isError && (
        <StateBlock message={getErrorMessage(error)} />
      )}
      {!isLoading && !isError && notes && notes.length === 0 && (
        <p className="muted" style={{ fontSize: '0.8rem', marginTop: 8 }}>
          Henüz not eklenmemiş.
        </p>
      )}
      {!isLoading && !isError && notes && notes.length > 0 && (
        <ul className="meeting-notes__list" style={{ marginTop: 8 }}>
          {notes.map((note) => (
            <li key={note.id} className="meeting-notes__item">
              <span className="meeting-notes__meta muted">
                {formatDateTime(note.createdAtUtc)} &nbsp;
                <strong>{note.authorName}</strong>
              </span>
              <p className="meeting-notes__content">{note.content}</p>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
