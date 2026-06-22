import type {
  MeetingMethod,
  MeetingStatus,
} from '../../../shared/types/enums';

export interface MeetingNoteDto {
  id: string;
  content: string;
  authorName: string;
  authorTitle: string | null;
  createdAtUtc: string;
}

export interface MeetingDto {
  id: string;
  companyId: string;
  companyTitle: string;
  contactId: string | null;
  contactName: string | null;
  salesRepId: string;
  salesRepName: string;
  salesRepTitle: string | null;
  date: string;
  time: string;
  address: string;
  method: MeetingMethod;
  status: MeetingStatus;
  comment: string | null;
  notes: MeetingNoteDto[];
}
