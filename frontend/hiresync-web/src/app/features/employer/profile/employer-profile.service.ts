import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  EmployerProfile,
  UpdateEmployerProfileRequest,
} from './employer-profile.models';

@Injectable({
  providedIn: 'root',
})
export class EmployerProfileService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = '/api/v1/employer/profile';

  getOwnProfile(): Observable<EmployerProfile> {
    return this.http.get<EmployerProfile>(this.endpoint);
  }

  updateOwnProfile(
    request: UpdateEmployerProfileRequest,
  ): Observable<EmployerProfile> {
    return this.http.put<EmployerProfile>(
      this.endpoint,
      request,
    );
  }
}