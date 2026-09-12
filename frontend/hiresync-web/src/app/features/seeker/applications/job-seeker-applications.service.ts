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
  JobSeekerApplicationPage,
  JobSeekerApplicationQuery,
} from './job-seeker-applications.models';

@Injectable({
  providedIn: 'root',
})
export class JobSeekerApplicationsService {
  private readonly http =
    inject(HttpClient);

  private readonly endpoint =
    '/api/v1/job-seeker/applications';

  getOwnApplications(
    query:
      JobSeekerApplicationQuery,
  ): Observable<JobSeekerApplicationPage> {
    let params =
      new HttpParams()
        .set(
          'page',
          query.page,
        )
        .set(
          'pageSize',
          query.pageSize,
        );

    if (query.status !== undefined)
    {
      params =
        params.set(
          'status',
          query.status,
        );
    }

    return this.http
      .get<JobSeekerApplicationPage>(
        this.endpoint,
        {
          params,
        },
      );
  }
}
