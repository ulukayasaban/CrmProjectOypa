import type { GoalProgressDto } from '../../goal/model/goal';

export type { GoalProgressDto };

export interface WeeklyDensityPoint {
  day: string;
  count: number;
}

export interface DashboardDto {
  activeLeads: number;
  totalCustomers: number;
  plannedMeetings: number;
  doneMeetings: number;
  weeklyDensity: WeeklyDensityPoint[];
  goals: GoalProgressDto[];
}
