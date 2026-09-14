export enum EmployerVerificationStatus {
  Pending = 1,
  Approved = 2,
  Rejected = 3,
}

export interface EmployerProfile {
  id: string;
  companyName: string;
  description: string;
  location: string;
  contactPersonName: string;
  contactPersonDesignation: string;
  businessRegistrationNumber: string;
  mobileNumber: string;
  companyWebsite: string | null;
  businessEmail: string;
  employerVerificationStatus: EmployerVerificationStatus;
  isProfileComplete: boolean;
  isVacancyReady: boolean;
}

export interface UpdateEmployerProfileRequest {
  companyName: string;
  description: string;
  location: string;
  contactPersonName: string;
  contactPersonDesignation: string;
  businessRegistrationNumber: string;
  mobileNumber: string;
  companyWebsite: string | null;
}