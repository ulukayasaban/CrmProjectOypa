export type Sector =
  | 'Tourism'
  | 'Retail'
  | 'FacilityManagement'
  | 'Energy'
  | 'Other';

export type CompanyType = 'Lead' | 'Customer';

export type LeadStatus = 'New' | 'Contacted' | 'Qualified' | 'Lost';

export type CustomerStatus = 'Active' | 'Passive';

export type CompanySource =
  | 'Referral'
  | 'Website'
  | 'Fair'
  | 'ColdCall'
  | 'Other';

export type MeetingMethod = 'Visit' | 'Phone' | 'Email';

export type MeetingStatus = 'Planned' | 'Done' | 'Cancelled';

export type UserRole = 'Admin' | 'Sales';
