import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import { meetingApi, type MeetingPayload } from '../api/meetingApi';
import type { MeetingStatus } from '../../../shared/types/enums';

interface AddMeetingNoteVars {
  meetingId: string;
  content: string;
  companyId?: string;
}

export function useMeetings() {
  return useQuery({
    queryKey: queryKeys.meetings,
    queryFn: meetingApi.getAll,
  });
}

export function useCreateMeeting() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: MeetingPayload) => meetingApi.create(payload),
    onSuccess: (meeting) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.meetings });
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
      // Hedef ilerlemesi tamamlanan görüşme sayısına bağlı; Hedefler ekranı da tazelensin.
      void queryClient.invalidateQueries({ queryKey: queryKeys.goals });
      void queryClient.invalidateQueries({ queryKey: queryKeys.mailDrafts });
      void queryClient.invalidateQueries({
        queryKey: queryKeys.companyMeetings(meeting.companyId),
      });
    },
  });
}

interface UpdateMeetingStatusVars {
  id: string;
  status: MeetingStatus;
  comment?: string;
  companyId?: string;
}

export function useUpdateMeetingStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status, comment }: UpdateMeetingStatusVars) =>
      meetingApi.updateStatus(id, status, comment),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.meetings });
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
      // Görüşme "Yapıldı" olunca hedef ilerlemesi değişir; Hedefler ekranı da tazelensin.
      void queryClient.invalidateQueries({ queryKey: queryKeys.goals });
      if (variables.companyId) {
        void queryClient.invalidateQueries({
          queryKey: queryKeys.companyMeetings(variables.companyId),
        });
      }
    },
  });
}

export function useAddMeetingNote() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ meetingId, content }: AddMeetingNoteVars) =>
      meetingApi.addNote(meetingId, content),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.meetings });
      if (variables.companyId) {
        void queryClient.invalidateQueries({
          queryKey: queryKeys.companyMeetings(variables.companyId),
        });
      }
    },
  });
}
