import {
  HttpClient,
} from '@angular/common/http';
import {
  inject,
  Injectable,
} from '@angular/core';
import { Observable } from 'rxjs';

import {
  ContactRequestResult,
  JobSeekerContactRequest,
  RespondContactRequestPayload,
} from './job-seeker-contact-requests.models';

@Injectable({
  providedIn: 'root',
})
export class JobSeekerContactRequestsService {
  private readonly http =
    inject(HttpClient);

  private readonly endpoint =
    '/api/v1/job-seeker/contact-requests';

  getOwnContactRequests():
    Observable<JobSeekerContactRequest[]> {
    return this.http
      .get<JobSeekerContactRequest[]>(
        this.endpoint,
      );
  }

  respondToContactRequest(
    contactRequestId: string,
    payload: RespondContactRequestPayload,
  ): Observable<ContactRequestResult> {
    return this.http
      .patch<ContactRequestResult>(
        `${this.endpoint}/${encodeURIComponent(contactRequestId)}/status`,
        payload,
      );
  }
}
