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
  PublicVacancyPage,
  VacancySearchQuery,
} from './vacancy-search.models';

@Injectable({
  providedIn: 'root',
})
export class VacancySearchService {
  private readonly http = inject(HttpClient);

  search(
    query: VacancySearchQuery,
  ): Observable<PublicVacancyPage> {
    let params = new HttpParams()
      .set('sort', query.sort)
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    const q = query.q?.trim();

    if (q) {
      params = params.set('q', q);
    }

    const location =
      query.location?.trim();

    if (location) {
      params =
        params.set(
          'location',
          location,
        );
    }

    return this.http.get<PublicVacancyPage>(
      '/api/v1/vacancies',
      {
        params,
      },
    );
  }
}