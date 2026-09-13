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
  AdminDashboardSummary,
} from './admin.models';
import {
  AdminService,
} from './admin.service';

@Component({
  selector:
    'app-admin-dashboard-page',
  standalone: true,
  imports: [
    DatePipe,
  ],
  templateUrl:
    './admin-dashboard-page.html',
  styleUrl:
    './admin-dashboard-page.css',
})
export class AdminDashboardPage {
  private readonly service =
    inject(AdminService);

  readonly isLoading =
    signal(false);

  readonly loadError =
    signal<string | null>(null);

  readonly dashboard =
    signal<
      AdminDashboardSummary | null
    >(null);

  constructor() {
    this.load();
  }

  reload(): void {
    this.load();
  }

  private load(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.service
      .getDashboard()
      .subscribe({
        next: (dashboard) => {
          this.dashboard.set(
            dashboard,
          );

          this.isLoading.set(
            false,
          );
        },

        error: (
          error: unknown,
        ) => {
          this.dashboard.set(
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

    return 'The Administrator dashboard could not be loaded. Please try again.';
  }
}