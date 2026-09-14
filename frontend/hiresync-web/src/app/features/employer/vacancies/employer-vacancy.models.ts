export type VacancyStatus = 1 | 2;

export type EducationLevel =
  | 0
  | 1
  | 2
  | 3
  | 4
  | 5
  | 6
  | 7
  | 8;

export type ApplicationStatus =
  | 1
  | 2
  | 3
  | 4
  | 5;

export type ContactRequestStatus =
  | 1
  | 2
  | 3;

export interface SkillSummary {
  id: string;
  name: string;
}

export interface EmployerVacancyListItem {
  id: string;
  title: string;
  location: string;
  status: VacancyStatus;
  publishedAtUtc: string;
  updatedAtUtc: string;
  closedAtUtc: string | null;
  rowVersion: string;
}

export interface EmployerVacancyPage {
  items: EmployerVacancyListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface EmployerVacancy {
  id: string;
  title: string;
  description: string;
  location: string;
  minimumExperienceMonths: number;
  requiredEducationLevel: EducationLevel | null;
  status: VacancyStatus;
  publishedAtUtc: string;
  updatedAtUtc: string;
  closedAtUtc: string | null;
  requiredSkills: SkillSummary[];
  rowVersion: string;
}

export interface CreateVacancyPayload {
  title: string;
  description: string;
  location: string;
  minimumExperienceMonths: number;
  requiredEducationLevel: EducationLevel | null;
  requiredSkillIds: string[];
}

export interface UpdateVacancyPayload
  extends CreateVacancyPayload {
  rowVersion: string;
}

export interface VacancyStatusResult {
  id: string;
  status: VacancyStatus;
  closedAtUtc: string | null;
  rowVersion: string;
}

export interface UpdateApplicationStatusPayload {
  status: ApplicationStatus;
  rowVersion: string;
}

export interface ContactRequestResult {
  id: string;
  jobApplicationId: string;
  status: ContactRequestStatus;
  requestedAtUtc: string;
  respondedAtUtc: string | null;
  rowVersion: string;
}

export interface ApplicationStatusResult {
  id: string;
  status: ApplicationStatus;
  updatedAtUtc: string;
  rowVersion: string;
}
export interface MatchSkill {
  id: string;
  name: string;
}

export interface RankedApplicantMatch {
  totalScore: number;
  skillsScore: number;
  experienceScore: number;
  educationScore: number;
  locationScore: number;
  matchedSkills: MatchSkill[];
  missingSkills: MatchSkill[];
}

export interface RankedApplicant {
  rank: number;
  applicationId: string;
  jobSeekerProfileId: string;
  jobSeekerDisplayName: string;
  status: ApplicationStatus;
  appliedAtUtc: string;
  updatedAtUtc: string;
  rowVersion: string;
  contactRequestStatus: ContactRequestStatus | null;
  match: RankedApplicantMatch;
}

export interface RankedApplicantPage {
  vacancyId: string;
  vacancyTitle: string;
  items: RankedApplicant[];
  page: number;
  pageSize: number;
  totalCount: number;
}
