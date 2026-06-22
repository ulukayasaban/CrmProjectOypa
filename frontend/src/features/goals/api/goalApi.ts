import { httpClient } from '../../../shared/api/httpClient';
import type { GoalDto, GoalWeekDto } from '../../../entities/goal/model/goal';
import type { GoalSegment } from '../../../entities/goal/model/goal';

export interface CreateGoalPayload {
  assigneeEmployeeId: string;
  segment: GoalSegment;
  weeklyTarget: number;
  title?: string;
}

export interface UpdateGoalPayload {
  assigneeEmployeeId: string;
  segment: GoalSegment;
  weeklyTarget: number;
  title?: string;
}

export const goalApi = {
  async getScoped(): Promise<GoalDto[]> {
    const { data } = await httpClient.get<GoalDto[]>('/goals');
    return data;
  },
  async create(payload: CreateGoalPayload): Promise<GoalDto> {
    const { data } = await httpClient.post<GoalDto>('/goals', payload);
    return data;
  },
  async update(id: string, payload: UpdateGoalPayload): Promise<GoalDto> {
    const { data } = await httpClient.put<GoalDto>(`/goals/${id}`, payload);
    return data;
  },
  async remove(id: string): Promise<void> {
    await httpClient.delete(`/goals/${id}`);
  },
  async getWeeks(id: string): Promise<GoalWeekDto[]> {
    const { data } = await httpClient.get<GoalWeekDto[]>(`/goals/${id}/weeks`);
    return data;
  },
};
