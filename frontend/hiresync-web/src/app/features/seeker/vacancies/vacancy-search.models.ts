export type VacancySearchSort =
  | 'Newest'
  | 'Match';

export interface VacancySearchQuery {
  q?: string;
  location?: string;
  sort: VacancySearchSort;
  page: number;
  pageSize: number;
}

export interface PublicVacancyListItem {
  id: string;
  title: string;
  companyName: string;
  location: string;
  minimumExperienceMonths: number;
  requiredEducationLevel: number | null;
  publishedAtUtc: string;
  matchScore: number | null;
}

export interface PublicVacancyPage {
  items: PublicVacancyListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}