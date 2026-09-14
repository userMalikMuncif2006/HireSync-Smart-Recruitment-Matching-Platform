export interface MatchSkill {
  id: string;
  name: string;
}

export interface MatchResult {
  totalScore: number;
  skillsScore: number;
  experienceScore: number;
  educationScore: number;
  locationScore: number;
  matchedSkills: MatchSkill[];
  missingSkills: MatchSkill[];
}

export type MatchStatus =
  | 'Ready'
  | 'ProfileIncomplete';

export interface PublicVacancyDetail {
  id: string;
  title: string;
  description: string;

  companyName: string;
  companyDescription: string;
  companyWebsite: string | null;
  companyLocation: string;

  location: string;
  minimumExperienceMonths: number;
  requiredEducationLevel: number | null;
  publishedAtUtc: string;

  requiredSkills: MatchSkill[];

  matchStatus: MatchStatus;
  match: MatchResult | null;
  missingProfileFields: string[];

  canApply: boolean;
  hasApplied: boolean;

  computedAtUtc: string;
}
export interface ApplicationCreated {
  id: string;
  vacancyId: string;
  status: number;
  appliedAtUtc: string;
  updatedAtUtc: string;
}

export interface ApiProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  code?: string;
}