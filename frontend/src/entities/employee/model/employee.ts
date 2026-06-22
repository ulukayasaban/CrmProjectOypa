export interface EmployeeDto {
  id: string;
  fullName: string | null;
  title: string;
  email: string | null;
  managerId: string | null;
  managerName: string | null;
  hasAccount: boolean;
  role: string | null;
}
