export type AdminAccountStatus =
  | 1
  | 2;

export type EmployerVerificationStatus =
  | 1
  | 2
  | 3;

export interface AdminDashboardSummary {
  totalUsers: number;
  totalVacancies: number;
  totalApplications: number;
  calculatedAtUtc: string;
}

export interface AdminUserListItem {
  id: string;
  displayName: string;
  email: string;
  role: string;
  accountStatus: AdminAccountStatus;
  createdAtUtc: string;
  rowVersion: string;
}

export interface AdminUserPage {
  items: AdminUserListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface AdminUserQuery {
  search?: string;
  status?: AdminAccountStatus | null;
  page: number;
  pageSize: number;
}

export interface UpdateAdminAccountStatusPayload {
  status: AdminAccountStatus;
  rowVersion: string;
}

export interface EmployerVerificationSummary {
  userId: string;
  email: string;
  displayName: string;
  status: EmployerVerificationStatus;
}