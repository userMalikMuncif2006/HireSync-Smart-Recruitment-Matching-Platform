import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  inject,
  signal,
} from '@angular/core';

import {
  EmployerVerificationSummary,
} from './admin.models';
import { AdminService } from './admin.service';

type VerificationDecision =
  | 'approve'
  | 'reject';

interface PendingDecision {
  employer:
    EmployerVerificationSummary;
  decision:
    VerificationDecision;
}

@Component({
  selector:
    'app-admin-employer-verification-page',
  standalone: true,
  templateUrl:
    './admin-employer-verification-page.html',
  styleUrl:
    './admin-employer-verification-page.css',
})
export class AdminEmployerVerificationPage {
  private readonly service =
    inject(AdminService);

  readonly isLoading =
    signal(false);

  readonly loadError =
    signal<string | null>(null);

  readonly actionError =
    signal<string | null>(null);

  readonly employers =
    signal<
      EmployerVerificationSummary[] |
      null
    >(null);

  readonly pendingDecision =
    signal<PendingDecision | null>(
      null,
    );

  readonly updatingId =
    signal<string | null>(null);

  constructor() {
    this.load();
  }

  reload(): void {
    this.load();
  }

  requestDecision(
    employer:
      EmployerVerificationSummary,
    decision:
      VerificationDecision,
  ): void {
    this.actionError.set(null);

    this.pendingDecision.set({
      employer,
      decision,
    });
  }

  cancelDecision(): void {
    this.pendingDecision.set(null);
  }

  confirmDecision(): void {
    const pending =
      this.pendingDecision();

    if (!pending) {
      return;
    }

    this.updatingId.set(
      pending.employer.userId,
    );

    this.actionError.set(null);

    const operation =
      pending.decision ===
        'approve'
        ? this.service
            .approveEmployer(
              pending.employer.userId,
            )
        : this.service
            .rejectEmployer(
              pending.employer.userId,
            );

    operation.subscribe({
      next: () => {
        this.pendingDecision.set(
          null,
        );

        this.updatingId.set(
          null,
        );

        this.load();
      },

      error: (
        error: unknown,
      ) => {
        this.actionError.set(
          this.readError(error),
        );

        this.updatingId.set(
          null,
        );
      },
    });
  }

  private load(): void {
    this.isLoading.set(true);
    this.loadError.set(null);
    this.actionError.set(null);

    this.service
      .getPendingEmployers()
      .subscribe({
        next: (employers) => {
          this.employers.set(
            employers,
          );

          this.isLoading.set(
            false,
          );
        },

        error: (
          error: unknown,
        ) => {
          this.employers.set(
            null,
          );

          this.loadError.set(
            this.readError(error),
          );

          this.isLoading.set(
            false,
          );
        },
      });
  }

  private readError(
    error: unknown,
  ): string {
    if (
      error instanceof
        HttpErrorResponse
    ) {
      const detail =
        error.error?.detail;

      if (
        typeof detail ===
          'string' &&
        detail.trim().length > 0
      ) {
        return detail;
      }
    }

    return 'The Employer verification operation could not be completed. Please try again.';
  }
}