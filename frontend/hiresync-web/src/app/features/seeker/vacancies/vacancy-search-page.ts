import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import {
  ActivatedRoute,
  Router,
} from '@angular/router';

import {
  PublicVacancyPage,
  VacancySearchQuery,
  VacancySearchSort,
} from './vacancy-search.models';
import { VacancySearchService } from './vacancy-search.service';

@Component({
  selector: 'app-vacancy-search-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DatePipe,
  ],
  templateUrl: './vacancy-search-page.html',
  styleUrls: [
    './vacancy-search-page.css',
    './vacancy-search-filters.css',
    './vacancy-search-cards.css',
    './vacancy-search-guidance.css',
  ],
})
export class VacancySearchPage {
  private readonly vacancySearchService =
    inject(VacancySearchService);

  private readonly route =
    inject(ActivatedRoute);

  private readonly router =
    inject(Router);

  private readonly destroyRef =
    inject(DestroyRef);

  private readonly pageSize = 20;

  readonly isLoading =
    signal(false);

  readonly errorMessage =
    signal<string | null>(null);

  readonly result =
    signal<PublicVacancyPage | null>(null);

  readonly form = new FormGroup({
    q: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.maxLength(100),
      ],
    }),
    location: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.maxLength(100),
      ],
    }),
    sort: new FormControl<VacancySearchSort>(
      'Newest',
      {
        nonNullable: true,
      },
    ),
  });

  constructor() {
    this.route.queryParamMap
      .pipe(
        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe((params) => {
        const query: VacancySearchQuery = {
          q: params.get('q') ?? undefined,
          location:
            params.get('location') ?? undefined,
          sort:
            params.get('sort') === 'Match'
              ? 'Match'
              : 'Newest',
          page: this.parsePage(
            params.get('page'),
          ),
          pageSize: this.pageSize,
        };

        this.form.setValue(
          {
            q: query.q ?? '',
            location:
              query.location ?? '',
            sort: query.sort,
          },
          {
            emitEvent: false,
          },
        );

        this.load(query);
      });
  }

  applyFilters(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.navigateToSearch(1);
  }

  clearFilters(): void {
    this.form.setValue({
      q: '',
      location: '',
      sort: 'Newest',
    });

    this.navigateToSearch(1);
  }

  reload(): void {
    const current =
      this.result();

    this.navigateToSearch(
      current?.page ?? 1,
    );
  }

  previousPage(): void {
    const current =
      this.result();

    if (!current ||
        current.page <= 1) {
      return;
    }

    this.navigateToSearch(
      current.page - 1,
    );
  }

  nextPage(): void {
    const current =
      this.result();

    if (!current ||
        current.page >=
          this.totalPages()) {
      return;
    }

    this.navigateToSearch(
      current.page + 1,
    );
  }

  openVacancy(
    vacancyId: string,
  ): void {
    void this.router.navigate(
      [
        '/seeker/vacancies',
        vacancyId,
      ],
      {
        queryParamsHandling:
          'preserve',
      },
    );
  }

  openProfile(): void {
    void this.router.navigate(
      [
        '/seeker/profile',
      ],
    );
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

  matchScoreLabel(
    score: number,
  ): string {
    return `${score.toFixed(2)}%`;
  }

  private navigateToSearch(
    page: number,
  ): void {
    const value =
      this.form.getRawValue();

    const q =
      value.q.trim();

    const location =
      value.location.trim();

    void this.router.navigate(
      [],
      {
        relativeTo: this.route,
        queryParams: {
          q:
            q.length > 0
              ? q
              : null,
          location:
            location.length > 0
              ? location
              : null,
          sort: value.sort,
          page,
        },
      },
    );
  }

  private load(
    query: VacancySearchQuery,
  ): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.result.set(null);

    this.vacancySearchService
      .search(query)
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.errorMessage.set(
            this.readErrorMessage(error),
          );

          this.isLoading.set(false);
        },
      });
  }

  private readErrorMessage(
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

    return 'Vacancies could not be loaded. Please try again.';
  }

  private parsePage(
    value: string | null,
  ): number {
    const parsed =
      Number(value);

    return Number.isInteger(parsed) &&
      parsed >= 1
      ? parsed
      : 1;
  }
}
