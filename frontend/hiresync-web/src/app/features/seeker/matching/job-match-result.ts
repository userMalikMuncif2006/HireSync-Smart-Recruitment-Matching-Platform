import {
  DatePipe,
} from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';

import {
  ApiProblemDetails,
  PublicVacancyDetail,
} from './job-match.models';
import { JobMatchService } from './job-match.service';

@Component({
  selector: 'app-job-match-result',
  standalone: true,
  imports: [
    DatePipe,
  ],
  templateUrl: './job-match-result.html',
  styleUrls: [
    './job-match-result.css',
    './job-match-result-scores.css',
  ],
})
export class JobMatchResult {
  readonly vacancyId =
    input.required<string>();

  readonly result =
    signal<PublicVacancyDetail | null>(null);

  readonly loading =
    signal(false);

  readonly applying =
    signal(false);

  readonly errorMessage =
    signal<string | null>(null);

  readonly applicationMessage =
    signal<string | null>(null);

  private readonly jobMatchService =
    inject(JobMatchService);

  constructor() {
    effect((onCleanup) => {
      const vacancyId =
        this.vacancyId().trim();

      this.result.set(null);
      this.errorMessage.set(null);
      this.applicationMessage.set(null);
      this.applying.set(false);

      if (!vacancyId) {
        this.loading.set(false);

        this.errorMessage.set(
          'A valid vacancy is required.',
        );

        return;
      }

      this.loading.set(true);

      const subscription =
        this.jobMatchService
          .getVacancyDetail(vacancyId)
          .subscribe({
            next: (result) => {
              this.result.set(result);
              this.loading.set(false);
            },
            error: () => {
              this.result.set(null);
              this.loading.set(false);

              this.errorMessage.set(
                'The vacancy detail could not be loaded.',
              );
            },
          });

      onCleanup(() => {
        subscription.unsubscribe();
      });
    });
  }

  companyInitial(
    companyName: string,
  ): string {
    const normalized =
      companyName.trim();

    return normalized.length > 0
      ? normalized.charAt(0).toUpperCase()
      : 'H';
  }

  experienceLabel(
    months: number,
  ): string {
    if (months === 0) {
      return 'No experience required';
    }

    if (months < 12) {
      return `${months} month${
        months === 1 ? '' : 's'
      }`;
    }

    const years =
      Math.floor(months / 12);

    const remainingMonths =
      months % 12;

    const yearLabel =
      `${years} year${
        years === 1 ? '' : 's'
      }`;

    if (remainingMonths === 0) {
      return yearLabel;
    }

    return `${yearLabel} ${remainingMonths} month${
      remainingMonths === 1
        ? ''
        : 's'
    }`;
  }

  educationLabel(
    level: number | null,
  ): string {
    switch (level) {
      case null:
        return 'No minimum';
      case 0:
        return 'No formal qualification';
      case 1:
        return 'Ordinary Level';
      case 2:
        return 'Advanced Level';
      case 3:
        return 'Certificate';
      case 4:
        return 'Diploma';
      case 5:
        return 'Bachelor';
      case 6:
        return 'Postgraduate Diploma';
      case 7:
        return 'Master';
      case 8:
        return 'Doctorate';
      default:
        return 'Not specified';
    }
  }

  apply(): void {
    const detail =
      this.result();

    if (
      detail === null ||
      !detail.canApply ||
      detail.hasApplied ||
      this.applying()
    ) {
      return;
    }

    const confirmed =
      window.confirm(
        `Apply for "${detail.title}" at ${detail.companyName}?`,
      );

    if (!confirmed) {
      return;
    }

    this.applicationMessage.set(null);
    this.applying.set(true);

    this.jobMatchService
      .applyToVacancy(detail.id)
      .subscribe({
        next: () => {
          this.result.update(
            current =>
              current === null
                ? null
                : {
                    ...current,
                    canApply: false,
                    hasApplied: true,
                  },
          );

          this.applicationMessage.set(
            'Application submitted successfully.',
          );

          this.applying.set(false);
        },
        error: (error: HttpErrorResponse) => {
          this.handleApplyError(error);
          this.applying.set(false);
        },
      });
  }

  private handleApplyError(
    error: HttpErrorResponse,
  ): void {
    const problem =
      (error.error ?? {}) as ApiProblemDetails;

    switch (problem.code) {
      case 'DUPLICATE_APPLICATION':
        this.result.update(
          current =>
            current === null
              ? null
              : {
                  ...current,
                  canApply: false,
                  hasApplied: true,
                },
        );

        this.applicationMessage.set(
          'You have already applied to this vacancy.',
        );

        return;

      case 'VACANCY_CLOSED':
        this.result.update(
          current =>
            current === null
              ? null
              : {
                  ...current,
                  canApply: false,
                },
        );

        this.applicationMessage.set(
          'This vacancy closed before your application could be submitted.',
        );

        return;

      case 'PROFILE_NOT_READY':
        this.applicationMessage.set(
          'Complete your structured profile before applying.',
        );

        return;

      case 'CURRENT_CV_REQUIRED':
        this.applicationMessage.set(
          'Upload a current CV before applying.',
        );

        return;

      case 'VACANCY_UNAVAILABLE':
        this.result.update(
          current =>
            current === null
              ? null
              : {
                  ...current,
                  canApply: false,
                },
        );

        this.applicationMessage.set(
          'This vacancy is no longer available.',
        );

        return;

      default:
        this.applicationMessage.set(
          problem.detail ??
            'The application could not be submitted. Please try again.',
        );
    }
  }
}
