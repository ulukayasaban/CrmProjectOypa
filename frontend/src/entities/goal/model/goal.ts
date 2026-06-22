export type GoalSegment = 'Customer' | 'Lead' | 'All';

/** Dashboard'da gösterilen hedef ilerleme özeti (GET /api/dashboard → goals[]). */
export interface GoalProgressDto {
  goalId: string;
  assigneeName: string | null;
  segment: GoalSegment;
  weeklyTarget: number;
  achieved: number;
  percent: number;
}

/** Hedef kaydı (GET /api/goals). İçinde bulunulan haftanın ilerlemesi düz alanlarda. */
export interface GoalDto {
  id: string;
  assigneeEmployeeId: string;
  assigneeName: string | null;
  segment: GoalSegment;
  weeklyTarget: number;
  title: string | null;
  isActive: boolean;
  currentTarget: number;
  currentAchieved: number;
  currentPercent: number;
}

export interface GoalWeekDto {
  weekStart: string;
  target: number;
  achieved: number;
  percent: number;
}
