import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { MatchResult } from './job-match.models';

@Injectable({
  providedIn: 'root',
})
export class JobMatchService {
  private readonly http = inject(HttpClient);

  getMatch(vacancyId: string): Observable<MatchResult> {
    return this.http.get<MatchResult>(
      `/api/v1/vacancies/${encodeURIComponent(vacancyId)}/match`,
    );
  }
}
