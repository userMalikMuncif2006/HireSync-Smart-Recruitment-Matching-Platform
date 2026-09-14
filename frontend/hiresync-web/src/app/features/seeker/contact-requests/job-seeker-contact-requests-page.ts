import {
  DatePipe,
} from '@angular/common';
import {
  HttpErrorResponse,
} from '@angular/common/http';
import {
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';

import {
  ContactRequestResponseStatus,
  ContactRequestStatus,
  JobSeekerContactRequest,
} from './job-seeker-contact-requests.models';
import {
  JobSeekerContactRequestsService,
} from './job-seeker-contact-requests.service';

type ContactRequestFilter =
  | 'all'
  | ContactRequestStatus;

@Component({
  selector:
    'app-job-seeker-contact-requests-page',
  standalone: true,
  imports: [
    DatePipe,
  ],
  templateUrl:
    './job-seeker-contact-requests-page.html',
  styleUrls: [
    './job-seeker-contact-requests-page.css',
    './job-seeker-contact-requests-list.css',
    './job-seeker-contact-requests-guidance.css',
  ],
})
export class JobSeekerContactRequestsPage {
  private readonly service =
    inject(JobSeekerContactRequestsService);

  readonly ContactRequestStatus =
    ContactRequestStatus;

  readonly isLoading =
    signal(false);

  readonly errorMessage =
    signal<string | null>(null);

  readonly requests =
    signal<JobSeekerContactRequest[]>([]);

  readonly selectedFilter =
    signal<ContactRequestFilter>('all');

  readonly respondingContactRequestId =
    signal<string | null>(null);

  readonly responseErrorMessage =
    signal<string | null>(null);

  readonly responseSuccessMessage =
    signal<string | null>(null);

  readonly pendingCount =
    computed(
      () =>
        this.requests().filter(
          request =>
            request.status ===
            ContactRequestStatus.Pending,
        ).length,
    );

  readonly acceptedCount =
    computed(
      () =>
        this.requests().filter(
          request =>
            request.status ===
            ContactRequestStatus.Accepted,
        ).length,
    );

  readonly declinedCount =
    computed(
      () =>
        this.requests().filter(
          request =>
            request.status ===
            ContactRequestStatus.Declined,
        ).length,
    );

  readonly filteredRequests =
    computed(() => {
      const selected =
        this.selectedFilter();

      if (selected === 'all') {
        return this.requests();
      }

      return this.requests().filter(
        request =>
          request.status === selected,
      );
    });

  constructor() {
    this.load();
  }

  retry(): void {
    this.load();
  }

  selectFilter(
    filter: ContactRequestFilter,
  ): void {
    this.selectedFilter.set(filter);
  }

  companyInitial(
    companyName: string,
  ): string {
    const normalized =
      companyName.trim();

    return normalized.length > 0
      ? normalized[0].toUpperCase()
      : 'H';
  }

  statusLabel(
    status: ContactRequestStatus,
  ): string {
    switch (status) {
      case ContactRequestStatus.Pending:
        return 'Pending';

      case ContactRequestStatus.Accepted:
        return 'Accepted';

      case ContactRequestStatus.Declined:
        return 'Declined';

      default:
        return 'Unknown';
    }
  }

  canRespond(
    request: JobSeekerContactRequest,
  ): boolean {
    return (
      request.status ===
        ContactRequestStatus.Pending &&
      this.respondingContactRequestId() ===
        null
    );
  }

  respond(
    request: JobSeekerContactRequest,
    status: ContactRequestResponseStatus,
  ): void {
    if (!this.canRespond(request)) {
      return;
    }

    if (
      status ===
        ContactRequestStatus.Declined &&
      !window.confirm(
        'Decline this contact request?',
      )
    ) {
      return;
    }

    this.respondingContactRequestId.set(
      request.id,
    );

    this.responseErrorMessage.set(null);
    this.responseSuccessMessage.set(null);

    this.service
      .respondToContactRequest(
        request.id,
        {
          status,
          rowVersion:
            request.rowVersion,
        },
      )
      .subscribe({
        next: () => {
          this.respondingContactRequestId
            .set(null);

          this.responseSuccessMessage.set(
            status ===
              ContactRequestStatus.Accepted
              ? 'Contact request accepted.'
              : 'Contact request declined.',
          );

          this.load();
        },
        error: (error: unknown) => {
          this.respondingContactRequestId
            .set(null);

          this.responseErrorMessage.set(
            this.readResponseError(error),
          );
        },
      });
  }

  private load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.service
      .getOwnContactRequests()
      .subscribe({
        next: (requests) => {
          this.requests.set(requests);
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.requests.set([]);

          this.errorMessage.set(
            this.readLoadError(error),
          );

          this.isLoading.set(false);
        },
      });
  }

  private readResponseError(
    error: unknown,
  ): string {
    if (error instanceof HttpErrorResponse) {
      const detail =
        error.error?.detail;

      if (
        typeof detail === 'string' &&
        detail.trim().length > 0
      ) {
        return detail;
      }

      if (error.status === 409) {
        return 'The contact request changed before your response could be saved. Reload and try again.';
      }
    }

    return 'Your contact request response could not be saved. Please try again.';
  }

  private readLoadError(
    error: unknown,
  ): string {
    if (error instanceof HttpErrorResponse) {
      const detail =
        error.error?.detail;

      if (
        typeof detail === 'string' &&
        detail.trim().length > 0
      ) {
        return detail;
      }
    }

    return 'Your contact requests could not be loaded. Please try again.';
  }
}
