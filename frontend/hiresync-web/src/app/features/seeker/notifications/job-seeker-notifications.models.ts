export enum NotificationType {
  ApplicationStatusChanged = 1,
}

export interface JobSeekerNotification {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  jobApplicationId: string;
  isRead: boolean;
  createdAtUtc: string;
  readAtUtc: string | null;
}

export interface NotificationPage {
  items: JobSeekerNotification[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface NotificationQuery {
  page: number;
  pageSize: number;
}
