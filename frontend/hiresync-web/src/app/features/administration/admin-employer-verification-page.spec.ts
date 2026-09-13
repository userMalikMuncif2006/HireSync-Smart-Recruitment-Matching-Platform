import { provideRouter } from '@angular/router';
import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  Observable,
  of,
} from 'rxjs';

import {
  EmployerVerificationSummary,
} from './admin.models';
import {
  AdminEmployerVerificationPage,
} from './admin-employer-verification-page';
import {
  AdminService,
} from './admin.service';

describe(
  'AdminEmployerVerificationPage',
  () => {
    let fixture:
      ComponentFixture<
        AdminEmployerVerificationPage
      >;

    let service:
      FakeAdminService;

    async function createComponent():
      Promise<void> {
      service =
        new FakeAdminService();

      await TestBed
        .configureTestingModule({
          imports: [
            AdminEmployerVerificationPage,
          ],
          providers: [
            provideRouter([]),
            {
              provide:
                AdminService,
              useValue:
                service,
            },
          ],
        })
        .compileComponents();

      fixture =
        TestBed.createComponent(
          AdminEmployerVerificationPage,
        );

      fixture.detectChanges();

      await fixture.whenStable();

      fixture.detectChanges();
    }

    afterEach(() => {
      vi.restoreAllMocks();
    });

    it('renders pending Employers', async () => {
      await createComponent();

      const text =
        fixture.nativeElement
          .textContent as string;

      expect(text)
        .toContain(
          'Example Company',
        );

      expect(text)
        .toContain(
          'company@example.com',
        );
    });

    it('approves and reloads the verification queue', async () => {
      await createComponent();

      const employer =
        service.employers[0];

      fixture.componentInstance
        .requestDecision(
          employer,
          'approve',
        );

      fixture.componentInstance
        .confirmDecision();

      expect(
        service.approvedIds,
      ).toEqual([
        employer.userId,
      ]);

      expect(
        service.loadCalls,
      ).toBe(2);
    });

    it('rejects and reloads the verification queue', async () => {
      await createComponent();

      const employer =
        service.employers[0];

      fixture.componentInstance
        .requestDecision(
          employer,
          'reject',
        );

      fixture.componentInstance
        .confirmDecision();

      expect(
        service.rejectedIds,
      ).toEqual([
        employer.userId,
      ]);

      expect(
        service.loadCalls,
      ).toBe(2);
    });
  },
);

class FakeAdminService {
  readonly employers:
    EmployerVerificationSummary[] =
      [
        {
          userId:
            '33333333-3333-3333-3333-333333333333',
          email:
            'company@example.com',
          displayName:
            'Example Company',
          status: 1,
        },
      ];

  readonly approvedIds:
    string[] = [];

  readonly rejectedIds:
    string[] = [];

  loadCalls = 0;

  getPendingEmployers():
    Observable<
      EmployerVerificationSummary[]
    > {
    this.loadCalls++;

    return of(
      this.employers,
    );
  }

  approveEmployer(
    employerUserId: string,
  ): Observable<
    EmployerVerificationSummary
  > {
    this.approvedIds.push(
      employerUserId,
    );

    return of({
      ...this.employers[0],
      status: 2,
    });
  }

  rejectEmployer(
    employerUserId: string,
  ): Observable<
    EmployerVerificationSummary
  > {
    this.rejectedIds.push(
      employerUserId,
    );

    return of({
      ...this.employers[0],
      status: 3,
    });
  }
}