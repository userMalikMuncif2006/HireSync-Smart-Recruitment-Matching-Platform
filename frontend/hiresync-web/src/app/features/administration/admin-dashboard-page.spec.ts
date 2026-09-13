import { provideRouter } from '@angular/router';
import {
  HttpErrorResponse,
} from '@angular/common/http';
import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  Observable,
  of,
  throwError,
} from 'rxjs';

import {
  AdminDashboardSummary,
} from './admin.models';
import {
  AdminDashboardPage,
} from './admin-dashboard-page';
import {
  AdminService,
} from './admin.service';

describe(
  'AdminDashboardPage',
  () => {
    let fixture:
      ComponentFixture<
        AdminDashboardPage
      >;

    async function createComponent(
      dashboard:
        Observable<
          AdminDashboardSummary
        >,
    ): Promise<void> {
      await TestBed
        .configureTestingModule({
          imports: [
            AdminDashboardPage,
          ],
          providers: [
            provideRouter([]),
            {
              provide:
                AdminService,
              useValue: {
                getDashboard:
                  () =>
                    dashboard,
              },
            },
          ],
        })
        .compileComponents();

      fixture =
        TestBed.createComponent(
          AdminDashboardPage,
        );

      fixture.detectChanges();

      await fixture.whenStable();

      fixture.detectChanges();
    }

    afterEach(() => {
      vi.restoreAllMocks();
    });

    it('renders authoritative system totals', async () => {
      await createComponent(
        of({
          totalUsers: 14,
          totalVacancies: 6,
          totalApplications: 21,
          calculatedAtUtc:
            '2026-09-13T05:30:00Z',
        }),
      );

      const text =
        fixture.nativeElement
          .textContent as string;

      expect(text)
        .toContain('Total users');

      expect(text)
        .toContain('14');

      expect(text)
        .toContain('Total vacancies');

      expect(text)
        .toContain('6');

      expect(text)
        .toContain('Total applications');

      expect(text)
        .toContain('21');
    });

    it('renders backend problem detail when dashboard loading fails', async () => {
      await createComponent(
        throwError(
          () =>
            new HttpErrorResponse({
              status: 500,
              error: {
                detail:
                  'Dashboard data is temporarily unavailable.',
              },
            }),
        ),
      );

      const text =
        fixture.nativeElement
          .textContent as string;

      expect(text)
        .toContain(
          'Unable to load dashboard',
        );

      expect(text)
        .toContain(
          'Dashboard data is temporarily unavailable.',
        );
    });
  },
);