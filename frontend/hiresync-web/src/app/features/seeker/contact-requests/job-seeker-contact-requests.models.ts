export enum ContactRequestStatus {
  Pending = 1,
  Accepted = 2,
  Declined = 3,
}

export type ContactRequestResponseStatus =
  | ContactRequestStatus.Accepted
  | ContactRequestStatus.Declined;

export interface JobSeekerContactRequest {
  id: string;
  jobApplicationId: string;
  vacancyId: string;
  vacancyTitle: string;
  employerCompanyName: string;
  status: ContactRequestStatus;
  requestedAtUtc: string;
  respondedAtUtc: string | null;
  rowVersion: string;
}

export interface RespondContactRequestPayload {
  status: ContactRequestResponseStatus;
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
