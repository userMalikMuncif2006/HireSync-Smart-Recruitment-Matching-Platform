export enum EducationLevel {
  NoFormalQualification = 0,
  OrdinaryLevel = 1,
  AdvancedLevel = 2,
  Certificate = 3,
  Diploma = 4,
  Bachelor = 5,
  PostgraduateDiploma = 6,
  Master = 7,
  Doctorate = 8,
}

export interface SkillSummary {
  id: string;
  name: string;
}

export interface JobSeekerProfile {
  totalExperienceMonths: number | null;
  educationLevel: EducationLevel | null;
  preferredLocation: string | null;
  skills: SkillSummary[];
  isMatchReady: boolean;
}

export interface UpdateJobSeekerProfileRequest {
  experienceMonths: number;
  educationLevel: EducationLevel;
  preferredLocation: string;
  skillIds: string[];
}

export interface JobSeekerCv {
  originalFileName: string;
  extension: string;
  contentType: string;
  sizeBytes: number;
  uploadedAtUtc: string;
}
