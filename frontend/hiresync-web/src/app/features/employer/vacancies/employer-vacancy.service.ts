import {
  HttpClient,
  HttpParams,
} from '@angular/common/http';
import {
  inject,
  Injectable,
} from '@angular/core';
import { Observable } from 'rxjs';

import {
  ApplicationStatus,
  ApplicationStatusResult,
  ContactRequestResult,
  CreateVacancyPayload,
  EmployerVacancy,
  EmployerVacancyPage,
  RankedApplicantPage,
  SkillSummary,
  UpdateApplicationStatusPayload,
  UpdateVacancyPayload,
  VacancyStatus,
  VacancyStatusResult,
} from './employer-vacancy.models';

@Injectable({
  providedIn: 'root',
})
export class EmployerVacancyService {
  private readonly http = inject(HttpClient);

  getVacancies(
    status: VacancyStatus | null,
    page: number,
    pageSize = 20,
  ): Observable<EmployerVacancyPage> {
    let params =
      new HttpParams()
        .set('page', page)
        .set('pageSize', pageSize);

    if (status !== null) {
      params =
        params.set(
          'status',
          status,
        );
    }

    return this.http.get<EmployerVacancyPage>(
      '/api/v1/employer/vacancies',
      {
        params,
      },
    );
  }

  getVacancy(
    vacancyId: string,
  ): Observable<EmployerVacancy> {
    return this.http.get<EmployerVacancy>(
      `/api/v1/employer/vacancies/${encodeURIComponent(vacancyId)}`,
    );
  }

  createVacancy(
    payload: CreateVacancyPayload,
  ): Observable<EmployerVacancy> {
    return this.http.post<EmployerVacancy>(
      '/api/v1/employer/vacancies',
      payload,
    );
  }

  updateVacancy(
    vacancyId: string,
    payload: UpdateVacancyPayload,
  ): Observable<EmployerVacancy> {
    return this.http.put<EmployerVacancy>(
      `/api/v1/employer/vacancies/${encodeURIComponent(vacancyId)}`,
      payload,
    );
  }

  getSkills(
    query?: string,
  ): Observable<SkillSummary[]> {
    let params =
      new HttpParams();

    const normalizedQuery =
      query?.trim();

    if (normalizedQuery) {
      params =
        params.set(
          'query',
          normalizedQuery,
        );
    }

    return this.http.get<SkillSummary[]>(
      '/api/v1/skills',
      {
        params,
      },
    );
  }

  closeVacancy(
    vacancyId: string,
    rowVersion: string,
  ): Observable<VacancyStatusResult> {
    return this.http.patch<VacancyStatusResult>(
      `/api/v1/employer/vacancies/${encodeURIComponent(vacancyId)}/status`,
      {
        status: 2,
        rowVersion,
      },
    );
  }

  createContactRequest(
    applicationId: string,
  ): Observable<ContactRequestResult> {
    return this.http.post<ContactRequestResult>(
      `/api/v1/employer/applications/${encodeURIComponent(applicationId)}/contact-requests`,
      null,
    );
  }

  updateApplicationStatus(
    applicationId: string,
    payload: UpdateApplicationStatusPayload,
  ): Observable<ApplicationStatusResult> {
    return this.http.patch<ApplicationStatusResult>(
      `/api/v1/employer/applications/${encodeURIComponent(applicationId)}/status`,
      payload,
    );
  }
  getRankedApplicants(
    vacancyId: string,
    status: ApplicationStatus | null,
    page: number,
    pageSize = 20,
  ): Observable<RankedApplicantPage> {
    let params =
      new HttpParams()
        .set('page', page)
        .set('pageSize', pageSize);

    if (status !== null) {
      params =
        params.set(
          'status',
          status,
        );
    }

    return this.http.get<RankedApplicantPage>(
      `/api/v1/employer/vacancies/${encodeURIComponent(vacancyId)}/applicants`,
      {
        params,
      },
    );
  }
}
