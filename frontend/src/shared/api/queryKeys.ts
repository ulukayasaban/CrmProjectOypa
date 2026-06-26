export const queryKeys = {
  leads: ['companies', 'leads'] as const,
  customers: ['companies', 'customers'] as const,
  company: (id: string) => ['companies', id] as const,
  companyContacts: (id: string) => ['companies', id, 'contacts'] as const,
  companyMeetings: (id: string) => ['companies', id, 'meetings'] as const,
  meetings: ['meetings'] as const,
  salesReps: ['salesreps'] as const,
  mailDrafts: ['maildrafts'] as const,
  mailDraftByMeeting: (meetingId: string) =>
    ['maildrafts', 'by-meeting', meetingId] as const,
  notifications: ['notifications'] as const,
  notificationsUnread: ['notifications', 'unread-count'] as const,
  dashboard: ['dashboard'] as const,
  goals: ['goals'] as const,
  me: ['auth', 'me'] as const,
  employees: ['employees'] as const,
  managedEmployees: ['employees', 'managed'] as const,
  tenders: ['tenders'] as const,
  tender: (id: string) => ['tenders', id] as const,

  // Sayfalı sorgular — her parametre seti ayrı cache entry oluşturur.
  // `object` tipi tüm arayüzleri karşılar; cast gerekmez.
  leadsPaged: (params: object) =>
    ['companies', 'leads', 'paged', params] as const,
  customersPaged: (params: object) =>
    ['companies', 'customers', 'paged', params] as const,
  meetingsPaged: (params: object) =>
    ['meetings', 'paged', params] as const,
  managedEmployeesPaged: (params: object) =>
    ['employees', 'managed', 'paged', params] as const,
  tendersPaged: (params: object) =>
    ['tenders', 'paged', params] as const,

  /** Admin kullanıcı listesi (/auth/users) */
  users: ['auth', 'users'] as const,

  /** Bildirim tür tercihleri (/notifications/preferences) */
  notificationPreferences: ['notifications', 'preferences'] as const,

  /** Kategori listesi (/categories) */
  categories: ['categories'] as const,

  /** Firma notları (/companies/{id}/notes) */
  companyNotes: (id: string) => ['companies', id, 'notes'] as const,
};
