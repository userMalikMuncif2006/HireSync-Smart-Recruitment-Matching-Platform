import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  ApplicationCreated,
  PublicVacancyDetail,
} from './job-match.models';

@Injectable({
  providedIn: 'root',
})
export class JobMatchService {
  private readonly http = inject(HttpClient);

  getVacancyDetail(
    vacancyId: string,
  ): Observable<PublicVacancyDetail> {
    return this.http.get<PublicVacancyDetail>(
      `/api/v1/vacancies/${encodeURIComponent(vacancyId)}`,
    );
  }

  applyToVacancy(
    vacancyId: string,
  ): Observable<ApplicationCreated> {
    return this.http.post<ApplicationCreated>(
      `/api/v1/vacancies/${encodeURIComponent(vacancyId)}/applications`,
      null,
    );
  }
}