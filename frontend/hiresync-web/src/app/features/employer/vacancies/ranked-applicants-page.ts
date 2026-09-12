import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
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
  ActivatedRoute,
  Router,
} from '@angular/router';

import {
  ApplicationStatus,
  ContactRequestStatus,
  RankedApplicant,
  RankedApplicantPage,
} from './employer-vacancy.models';
import { EmployerVacancyService } from './employer-vacancy.service';

@Component({
  selector: 'app-ranked-applicants-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DatePipe,
  ],
  templateUrl: './ranked-applicants-page.html',
  styleUrl: './ranked-applicants-page.css',
})
export class RankedApplicantsPage {
  private readonly service =
    inject(EmployerVacancyService);

  private readonly route =
    inject(ActivatedRoute);

  private readonly router =
    inject(Router);

  private readonly pageSize = 20;

  private readonly vacancyId =
    this.route.snapshot.paramMap.get(
      'vacancyId',
    ) ?? '';

  readonly statusFilter =
    new FormControl<
      'all' | '1' | '2' | '3' | '4' | '5'
    >(
      'all',
      {
        nonNullable: true,
      },
    );

  readonly isLoading =
    signal(false);

  readonly errorMessage =
    signal<string | null>(null);

  readonly result =
    signal<RankedApplicantPage | null>(
      null,
    );

  readonly selectedStatuses =
    signal<Record<string, ApplicationStatus>>(
      {},
    );

  readonly updatingApplicationId =
    signal<string | null>(null);

  readonly updateErrorMessage =
    signal<string | null>(null);

  readonly updateSuccessMessage =
    signal<string | null>(null);

  constructor() {
    if (!this.vacancyId) {
      this.errorMessage.set(
        'The vacancy identifier is invalid.',
      );
      return;
    }

    this.load(1);
  }

  applyFilter(): void {
    this.load(1);
  }

  reload(): void {
    this.load(
      this.result()?.page ?? 1,
    );
  }

  previousPage(): void {
    const current =
      this.result();

    if (!current ||
        current.page <= 1) {
      return;
    }

    this.load(current.page - 1);
  }

  nextPage(): void {
    const current =
      this.result();

    if (!current ||
        current.page >=
          this.totalPages()) {
      return;
    }

    this.load(current.page + 1);
  }

  totalPages(): number {
    const current =
      this.result();

    if (!current ||
        current.totalCount === 0) {
      return 1;
    }

    return Math.ceil(
      current.totalCount /
        current.pageSize,
    );
  }

  backToVacancies(): void {
    void this.router.navigate([
      '/employer/vacancies',
    ]);
  }

  applicationStatusLabel(
    status: ApplicationStatus,
  ): string {
    switch (status) {
      case 1:
        return 'Applied';
      case 2:
        return 'Under review';
      case 3:
        return 'Shortlisted';
      case 4:
        return 'Selected';
      case 5:
        return 'Rejected';
    }
  }

  selectedApplicationStatus(
    applicant: RankedApplicant,
  ): ApplicationStatus {
    return this.selectedStatuses()[
      applicant.applicationId
    ] ?? applicant.status;
  }

  changeSelectedApplicationStatus(
    applicationId: string,
    value: string,
  ): void {
    const status =
      Number(value) as ApplicationStatus;

    this.selectedStatuses.update(
      (current) => ({
        ...current,
        [applicationId]: status,
      }),
    );
  }

  isTerminalStatus(
    status: ApplicationStatus,
  ): boolean {
    return status === 4 || status === 5;
  }

  canUpdateApplicationStatus(
    applicant: RankedApplicant,
  ): boolean {
    return (
      !this.isTerminalStatus(
        applicant.status,
      ) &&
      this.selectedApplicationStatus(
        applicant,
      ) !== applicant.status &&
      this.updatingApplicationId() === null
    );
  }

  updateApplicationStatus(
    applicant: RankedApplicant,
  ): void {
    if (
      !this.canUpdateApplicationStatus(
        applicant,
      )
    ) {
      return;
    }

    const status =
      this.selectedApplicationStatus(
        applicant,
      );

    this.updatingApplicationId.set(
      applicant.applicationId,
    );

    this.updateErrorMessage.set(null);
    this.updateSuccessMessage.set(null);

    this.service
      .updateApplicationStatus(
        applicant.applicationId,
        {
          status,
          rowVersion:
            applicant.rowVersion,
        },
      )
      .subscribe({
        next: () => {
          this.updatingApplicationId.set(
            null,
          );

          this.updateSuccessMessage.set(
            'Application status updated successfully.',
          );

          this.load(
            this.result()?.page ?? 1,
          );
        },
        error: (error: unknown) => {
          this.updatingApplicationId.set(
            null,
          );

          this.updateErrorMessage.set(
            this.readUpdateError(error),
          );
        },
      });
  }

  contactStatusLabel(
    status: ContactRequestStatus | null,
  ): string {
    switch (status) {
      case 1:
        return 'Contact pending';
      case 2:
        return 'Contact accepted';
      case 3:
        return 'Contact declined';
      default:
        return 'No contact request';
    }
  }

  scoreLabel(
    score: number,
  ): string {
    return `${score.toFixed(2)}%`;
  }

  componentScoreLabel(
    score: number,
    maximum: number,
  ): string {
    return `${score.toFixed(2)} / ${maximum}`;
  }

  private load(
    page: number,
  ): void {
    if (!this.vacancyId) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.service
      .getRankedApplicants(
        this.vacancyId,
        this.selectedStatus(),
        page,
        this.pageSize,
      )
      .subscribe({
        next: (result) => {
          this.result.set(result);

          this.selectedStatuses.set(
            Object.fromEntries(
              result.items.map(
                (applicant) => [
                  applicant.applicationId,
                  applicant.status,
                ],
              ),
            ),
          );
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.result.set(null);
          this.errorMessage.set(
            this.readError(error),
          );
          this.isLoading.set(false);
        },
      });
  }

  private selectedStatus():
    ApplicationStatus | null {
    if (
      this.statusFilter.value ===
      'all'
    ) {
      return null;
    }

    return Number(
      this.statusFilter.value,
    ) as ApplicationStatus;
  }

  private readUpdateError(
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
        return 'This application changed before the update could be saved. Reload the applicants and try again.';
      }
    }

    return 'The application status could not be updated. Please try again.';
  }

  private readError(
    error: unknown,
  ): string {
    if (error instanceof HttpErrorResponse) {
      const detail =
        error.error?.detail;

      if (typeof detail === 'string' &&
          detail.trim().length > 0) {
        return detail;
      }
    }

    return 'Ranked applicants could not be loaded. Please try again.';
  }
}
