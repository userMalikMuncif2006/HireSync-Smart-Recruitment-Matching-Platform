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