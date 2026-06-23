/**
 * Tip daraltma (type-narrowing) yardımcıları.
 * `as` cast'lerinin yerini runtime-güvenli kontroller alır.
 * `any` kullanılmaz; bilinmeyen değer için güvenli varsayılan döner.
 */

import type { Sector, LeadStatus, UserRole } from '../types/enums';
import type { TenderStatus } from '../../entities/tender/model/tender';

// ---------------------------------------------------------------------------
// Sector
// ---------------------------------------------------------------------------
const VALID_SECTORS: ReadonlySet<string> = new Set<Sector>([
  'Tourism',
  'Retail',
  'FacilityManagement',
  'Energy',
  'Other',
]);

/** Verilen string bir Sector değeri mi? */
export function isSector(value: string): value is Sector {
  return VALID_SECTORS.has(value);
}

/**
 * Select onChange'den gelen değeri `Sector | ''` olarak döner.
 * Tanınan bir Sector değilse boş string (= "tümü") olarak yorumlar.
 */
export function toSectorFilter(value: string): Sector | '' {
  return isSector(value) ? value : '';
}

// ---------------------------------------------------------------------------
// LeadStatus
// ---------------------------------------------------------------------------
const VALID_LEAD_STATUSES: ReadonlySet<string> = new Set<LeadStatus>([
  'New',
  'Contacted',
  'Qualified',
  'Lost',
]);

/** Verilen string bir LeadStatus değeri mi? */
export function isLeadStatus(value: string): value is LeadStatus {
  return VALID_LEAD_STATUSES.has(value);
}

/**
 * Select onChange'den gelen değeri `LeadStatus` olarak döner.
 * Geçersiz değerde `undefined` döner; çağıran yer mutasyon yapmaz.
 */
export function toLeadStatus(value: string): LeadStatus | undefined {
  return isLeadStatus(value) ? value : undefined;
}

// ---------------------------------------------------------------------------
// TenderStatus
// ---------------------------------------------------------------------------
const VALID_TENDER_STATUSES: ReadonlySet<string> = new Set<TenderStatus>([
  'Hazirlik',
  'TeklifVerildi',
  'Kazanildi',
  'Kaybedildi',
  'Iptal',
]);

/** Verilen string bir TenderStatus değeri mi? */
export function isTenderStatus(value: string): value is TenderStatus {
  return VALID_TENDER_STATUSES.has(value);
}

// ---------------------------------------------------------------------------
// UserRole
// ---------------------------------------------------------------------------
const VALID_USER_ROLES: ReadonlySet<string> = new Set<UserRole>([
  'Admin',
  'Sales',
]);

/** Verilen string bir UserRole değeri mi? */
export function isUserRole(value: string): value is UserRole {
  return VALID_USER_ROLES.has(value);
}

/**
 * `EmployeeDto.role` alanı `string | null` olarak gelir.
 * `AssignRoleFormValues['role']` için güvenli daraltma: null/bilinmeyen → undefined.
 */
export function toUserRole(value: string | null | undefined): UserRole | undefined {
  if (value == null) return undefined;
  return isUserRole(value) ? value : undefined;
}
