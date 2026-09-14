import {
  DatePipe,
} from '@angular/common';
import {
  HttpErrorResponse,
} from '@angular/common/http';
import {
  Component,
  inject,
  signal,
} from '@angular/core';
import {
  FormControl,
  ReactiveFormsModule,
} from '@angular/forms';
import {
  Router,
} from '@angular/router';

import {
  ApplicationStatus,
  JobSeekerApplicationPage,
  VacancyStatus,
} from './job-seeker-applications.models';
import {
  JobSeekerApplicationsService,
} from './job-seeker-applications.service';

@Component({
  selector:
    'app-job-seeker-applications-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DatePipe,
  ],
  templateUrl:
    './job-seeker-applications-page.html',
  styleUrls: [
    './job-seeker-applications-page.css',
    './job-seeker-applications-content.css',
    './job-seeker-applications-layout.css',
    './job-seeker-applications-cards.css',
    './job-seeker-applications-guidance.css',
    './job-seeker-applications-hero.css',
  ],
})
export class JobSeekerApplicationsPage {
  private readonly service =
    inject(JobSeekerApplicationsService);

  private readonly router =
    inject(Router);

  private readonly pageSize =
    20;

  readonly ApplicationStatus =
    ApplicationStatus;

  readonly result =
    signal<JobSeekerApplicationPage | null>(
      null,
    );

  readonly isLoading =
    signal(false);

  readonly errorMessage =
    signal<string | null>(null);

  readonly statusControl =
    new FormControl<
      ApplicationStatus | null
    >(null);

  constructor() {
    this.load(1);
  }

  applyStatusFilter(): void {
    this.load(1);
  }

  selectStatusFilter(
    status:
      ApplicationStatus | null,
  ): void {
    this.statusControl.setValue(
      status,
    );

    this.load(1);
  }

  isStatusSelected(
    status:
      ApplicationStatus | null,
  ): boolean {
    return (
      this.statusControl.value ===
      status
    );
  }

  selectedStatusLabel(): string {
    const status =
      this.statusControl.value;

    return status === null
      ? 'All statuses'
      : this.applicationStatusLabel(
          status,
        );
  }

  clearStatusFilter(): void {
    this.statusControl.setValue(
      null,
    );

    this.load(1);
  }

  retry(): void {
    this.load(
      this.result()?.page ?? 1,
    );
  }

  previousPage(): void {
    const current =
      this.result();

    if (!current ||
        current.page <= 1)
    {
      return;
    }

    this.load(
      current.page - 1,
    );
  }

  nextPage(): void {
    const current =
      this.result();

    if (!current ||
        current.page >=
          this.totalPages())
    {
      return;
    }

    this.load(
      current.page + 1,
    );
  }

  openVacancy(
    vacancyId: string,
  ): void {
    void this.router.navigate([
      '/seeker/vacancies',
      vacancyId,
    ]);
  }

  browseVacancies(): void {
    void this.router.navigate([
      '/seeker/vacancies',
    ]);
  }

  openNotifications(): void {
    void this.router.navigate([
      '/seeker/notifications',
    ]);
  }

  totalPages(): number {
    const current =
      this.result();

    if (!current ||
        current.totalCount === 0)
    {
      return 1;
    }

    return Math.ceil(
      current.totalCount /
        current.pageSize,
    );
  }

  companyInitial(
    companyName: string,
  ): string {
    const trimmed =
      companyName.trim();

    return trimmed.length > 0
      ? trimmed[0].toUpperCase()
      : 'H';
  }

  applicationStatusLabel(
    status:
      ApplicationStatus,
  ): string {
    switch (status)
    {
      case ApplicationStatus.Applied:
        return 'Applied';

      case ApplicationStatus.UnderReview:
        return 'Under review';

      case ApplicationStatus.Shortlisted:
        return 'Shortlisted';

      case ApplicationStatus.Selected:
        return 'Selected';

      case ApplicationStatus.Rejected:
        return 'Rejected';

      default:
        return 'Unknown';
    }
  }

  vacancyStatusLabel(
    status:
      VacancyStatus,
  ): string {
    return status ===
      VacancyStatus.Closed
        ? 'Closed'
        : 'Open';
  }

  private load(
    page: number,
  ): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const status =
      this.statusControl.value ??
      undefined;

    this.service
      .getOwnApplications({
        status,
        page,
        pageSize:
          this.pageSize,
      })
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.isLoading.set(false);
        },
        error: (
          error:
            HttpErrorResponse,
        ) => {
          this.errorMessage.set(
            this.problemMessage(
              error,
              'Your applications could not be loaded.',
            ),
          );

          this.isLoading.set(false);
        },
      });
  }

  private problemMessage(
    error:
      HttpErrorResponse,
    fallback:
      string,
  ): string {
    const body =
      error.error as
        | {
            detail?: unknown;
          }
        | null;

    if (
      body &&
      typeof body.detail ===
        'string' &&
      body.detail.trim()
        .length > 0
    )
    {
      return body.detail;
    }

    return fallback;
  }
}
