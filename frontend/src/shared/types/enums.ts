export type Sector =
  | 'Tourism'
  | 'Retail'
  | 'FacilityManagement'
  | 'Energy'
  | 'Other';

/** OYPA'nın hizmet verdiği sektör (firma sektöründen ayrı kavram). */
export type ServiceSector = 'TesisYonetimi' | 'Turizm' | 'Perakende';

/** Firmanın OYAK Grubu içinden mi dışından mı olduğunu belirtir. */
export type FirmType = 'IcFirma' | 'DisFirma';

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
