export enum ApplicationStatus {
  Applied = 1,
  UnderReview = 2,
  Shortlisted = 3,
  Selected = 4,
  Rejected = 5,
}

export enum VacancyStatus {
  Open = 1,
  Closed = 2,
}

export interface JobSeekerApplicationListItem {
  applicationId: string;
  vacancyId: string;
  vacancyTitle: string;
  companyName: string;
  vacancyLocation: string;
  vacancyStatus: VacancyStatus;
  status: ApplicationStatus;
  appliedAtUtc: string;
  updatedAtUtc: string;
}

export interface JobSeekerApplicationPage {
  items: JobSeekerApplicationListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface JobSeekerApplicationQuery {
  status?: ApplicationStatus;
  page: number;
  pageSize: number;
}
