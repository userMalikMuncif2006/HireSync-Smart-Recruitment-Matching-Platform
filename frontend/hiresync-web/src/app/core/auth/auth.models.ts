export type AuthRole =
  | 'JobSeeker'
  | 'Employer'
  | 'Administrator';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  userId: string;
  email: string;
  role: AuthRole;
}

export interface RegisterJobSeekerRequest {
  email: string;
  password: string;
  displayName: string;
}

export interface RegisterJobSeekerResponse {
  userId: string;
  email: string;
  displayName: string;
  role: 'JobSeeker';
}

export interface RegisterEmployerRequest {
  email: string;
  password: string;
  companyName: string;
  description: string;
  location: string;
  contactPersonName: string;
  contactPersonDesignation: string;
  businessRegistrationNumber: string;
  mobileNumber: string;
  companyWebsite: string | null;
}

export interface RegisterEmployerResponse {
  userId: string;
  employerProfileId: string;
  email: string;
  role: 'Employer';
  employerVerificationStatus: number | string;
}

export interface JobSeekerOtpRequest {
  email: string;
}

export interface JobSeekerOtpVerifyRequest {
  email: string;
  code: string;
}

export interface JobSeekerOtpVerificationResponse {
  succeeded: boolean;
  failureReason: number | string | null;
}
export interface EmployerOtpRequest {
  email: string;
}

export interface EmployerOtpVerifyRequest {
  email: string;
  code: string;
}

export interface OtpRequestResponse {
  succeeded: boolean;
  expiresAtUtc: string | null;
  failureReason: number | string | null;
  retryAfterSeconds: number | null;
}

export interface EmployerOtpVerificationResponse {
  succeeded: boolean;
  failureReason: number | string | null;
}

export interface AdministratorActivationRequest {
  email: string;
  password: string;
}

export interface AdministratorActivationVerifyRequest {
  email: string;
  password: string;
  code: string;
}

export interface AdministratorActivationRequestResponse {
  succeeded: boolean;
  expiresAtUtc: string | null;
  failureReason: number | string | null;
  retryAfterSeconds: number | null;
}

export interface AdministratorActivationVerificationResponse {
  succeeded: boolean;
  failureReason: number | string | null;
}
