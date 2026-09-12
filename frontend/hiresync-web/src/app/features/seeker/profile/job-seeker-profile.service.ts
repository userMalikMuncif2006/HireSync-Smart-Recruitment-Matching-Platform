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
  JobSeekerCv,
  JobSeekerProfile,
  SkillSummary,
  UpdateJobSeekerProfileRequest,
} from './job-seeker-profile.models';

@Injectable({
  providedIn: 'root',
})
export class JobSeekerProfileService {
  private readonly http =
    inject(HttpClient);

  private readonly profileEndpoint =
    '/api/v1/job-seeker/profile';

  private readonly skillsEndpoint =
    '/api/v1/skills';

  private readonly cvEndpoint =
    '/api/v1/job-seeker/cv';

  getOwnProfile():
    Observable<JobSeekerProfile> {
    return this.http.get<JobSeekerProfile>(
      this.profileEndpoint,
    );
  }

  updateOwnProfile(
    request:
      UpdateJobSeekerProfileRequest,
  ): Observable<JobSeekerProfile> {
    return this.http.put<JobSeekerProfile>(
      this.profileEndpoint,
      request,
    );
  }

  lookupSkills(
    query?: string,
  ): Observable<SkillSummary[]> {
    const trimmed =
      query?.trim() ?? '';

    const options =
      trimmed.length === 0
        ? {}
        : {
            params:
              new HttpParams().set(
                'query',
                trimmed,
              ),
          };

    return this.http.get<SkillSummary[]>(
      this.skillsEndpoint,
      options,
    );
  }

  getOwnCv():
    Observable<JobSeekerCv> {
    return this.http.get<JobSeekerCv>(
      this.cvEndpoint,
    );
  }

  uploadOwnCv(
    file: File,
  ): Observable<JobSeekerCv> {
    const formData =
      new FormData();

    formData.append(
      'file',
      file,
    );

    return this.http.post<JobSeekerCv>(
      this.cvEndpoint,
      formData,
    );
  }

  downloadOwnCv():
    Observable<Blob> {
    return this.http.get(
      `${this.cvEndpoint}/file`,
      {
        responseType: 'blob',
      },
    );
  }
}
