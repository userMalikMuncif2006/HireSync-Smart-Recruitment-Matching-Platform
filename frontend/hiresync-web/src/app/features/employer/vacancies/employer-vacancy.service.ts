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
  EmployerVacancyPage,
  RankedApplicantPage,
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