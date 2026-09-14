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
import { Router } from '@angular/router';

import {
  EmployerVacancyListItem,
  EmployerVacancyPage,
  VacancyStatus,
} from './employer-vacancy.models';
import { EmployerVacancyService } from './employer-vacancy.service';

@Component({
  selector: 'app-employer-vacancy-list-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DatePipe,
  ],
  templateUrl: './employer-vacancy-list-page.html',
  styleUrls: ['./employer-vacancy-list-page.css', './employer-vacancy-list-cards.css', './employer-vacancy-list-hero-final.css', './employer-vacancy-list-hero-responsive.css'],
})
export class EmployerVacancyListPage {
  private readonly service =
    inject(EmployerVacancyService);

  private readonly router =
    inject(Router);

  private readonly pageSize = 20;

  readonly statusFilter =
    new FormControl<
      'all' | 'open' | 'closed'
    >(
      'all',
      {
        nonNullable: true,
      },
    );

  readonly isLoading =
    signal(false);

  readonly loadError =
    signal<string | null>(null);

  readonly actionError =
    signal<string | null>(null);

  readonly result =
    signal<EmployerVacancyPage | null>(
      null,
    );

  readonly pendingCloseId =
    signal<string | null>(null);

  readonly closingId =
    signal<string | null>(null);

  constructor() {
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

  statusLabel(
    status: VacancyStatus,
  ): string {
    return status === 1
      ? 'Open'
      : 'Closed';
  }

  requestClose(
    vacancyId: string,
  ): void {
    this.actionError.set(null);
    this.pendingCloseId.set(vacancyId);
  }

  cancelClose(): void {
    this.pendingCloseId.set(null);
  }

  confirmClose(
    vacancy: EmployerVacancyListItem,
  ): void {
    if (vacancy.status !== 1 ||
        this.pendingCloseId() !==
          vacancy.id) {
      return;
    }

    this.closingId.set(vacancy.id);
    this.actionError.set(null);

    this.service
      .closeVacancy(
        vacancy.id,
        vacancy.rowVersion,
      )
      .subscribe({
        next: () => {
          this.pendingCloseId.set(null);
          this.closingId.set(null);

          this.load(
            this.result()?.page ?? 1,
          );
        },
        error: (error: unknown) => {
          this.actionError.set(
            this.readError(error),
          );

          this.closingId.set(null);
        },
      });
  }

  createVacancy(): void {
    void this.router.navigate([
      '/employer/vacancies/new',
    ]);
  }

  editVacancy(
    vacancyId: string,
  ): void {
    void this.router.navigate([
      '/employer/vacancies',
      vacancyId,
      'edit',
    ]);
  }

  viewApplicants(
    vacancyId: string,
  ): void {
    void this.router.navigate([
      '/employer/vacancies',
      vacancyId,
      'applicants',
    ]);
  }

  private load(
    page: number,
  ): void {
    this.isLoading.set(true);
    this.loadError.set(null);
    this.actionError.set(null);
    this.pendingCloseId.set(null);

    this.service
      .getVacancies(
        this.selectedStatus(),
        page,
        this.pageSize,
      )
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.result.set(null);
          this.loadError.set(
            this.readError(error),
          );
          this.isLoading.set(false);
        },
      });
  }

  private selectedStatus():
    VacancyStatus | null {
    switch (
      this.statusFilter.value
    ) {
      case 'open':
        return 1;
      case 'closed':
        return 2;
      default:
        return null;
    }
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

    return 'The vacancy operation could not be completed. Please try again.';
  }
}