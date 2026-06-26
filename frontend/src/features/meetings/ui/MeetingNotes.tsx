import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { noteSchema, type NoteFormValues } from '../model/meetingSchema';
import { useAddMeetingNote } from '../model/useMeetings';
import { FieldError } from '../../../shared/components/FieldError';
import { fieldAria } from '../../../shared/lib/fieldAria';
import { getErrorMessage } from '../../../shared/lib/errorMessage';
import { formatDateTime } from '../../../shared/lib/format';
import type { MeetingNoteDto } from '../../../entities/meeting/model/meeting';

interface MeetingNotesProps {
  meetingId: string;
  companyId?: string;
  notes: MeetingNoteDto[];
}

/**
 * Displays meeting notes in chronological order and provides an inline
 * "Not Ekle" form (RHF + zod). Used in CompanyDetailPage and MeetingHistoryPage.
 */
export function MeetingNotes({ meetingId, companyId, notes }: MeetingNotesProps) {
  const addNote = useAddMeetingNote();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<NoteFormValues>({
    resolver: zodResolver(noteSchema),
  });

  const onSubmit = handleSubmit(async (values) => {
    await addNote.mutateAsync({ meetingId, content: values.content, companyId });
    reset();
  });

  return (
    <div className="meeting-notes">
      {notes.length === 0 ? (
        <p className="muted" style={{ fontSize: '0.8rem', marginBottom: 8 }}>
          Henüz not yok.
        </p>
      ) : (
        <ul className="meeting-notes__list">
          {notes.map((note) => (
            <li key={note.id} className="meeting-notes__item">
              <span className="meeting-notes__meta muted">
                {formatDateTime(note.createdAtUtc)} &nbsp;
                <strong>{note.authorName}</strong>
                {note.authorTitle ? ` (${note.authorTitle})` : ''}
              </span>
              <p className="meeting-notes__content">{note.content}</p>
            </li>
          ))}
        </ul>
      )}

      <form className="meeting-notes__form" onSubmit={onSubmit}>
        {addNote.isError && (
          <div className="form-error" style={{ fontSize: '0.8rem', marginBottom: 4 }}>
            {getErrorMessage(addNote.error)}
          </div>
        )}
        <div style={{ display: 'flex', gap: 6, alignItems: 'flex-start' }}>
          <div style={{ flex: 1 }}>
            <textarea
              rows={2}
              placeholder="Not ekle..."
              style={{ width: '100%', resize: 'vertical', fontSize: '0.8rem' }}
              {...fieldAria('content', !!errors.content)}
              {...register('content')}
            />
            <FieldError id="content-error" message={errors.content?.message} />
          </div>
          <button
            type="submit"
            className="btn btn-ghost btn-sm"
            disabled={isSubmitting || addNote.isPending}
            style={{ whiteSpace: 'nowrap', marginTop: 2 }}
          >
            {isSubmitting || addNote.isPending ? '...' : 'Not Ekle'}
          </button>
        </div>
      </form>
    </div>
  );
}
