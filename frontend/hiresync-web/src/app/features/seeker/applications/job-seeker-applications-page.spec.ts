import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  Router,
} from '@angular/router';
import {
  Observable,
  of,
} from 'rxjs';

import {
  ApplicationStatus,
  JobSeekerApplicationPage,
  JobSeekerApplicationQuery,
  VacancyStatus,
} from './job-seeker-applications.models';
import {
  JobSeekerApplicationsPage,
} from './job-seeker-applications-page';
import {
  JobSeekerApplicationsService,
} from './job-seeker-applications.service';

describe(
  'JobSeekerApplicationsPage',
  () => {
    let fixture:
      ComponentFixture<
        JobSeekerApplicationsPage
      >;

    let service:
      FakeApplicationsService;

    let navigate:
      ReturnType<typeof vi.fn>;

    async function createComponent(
      response:
        JobSeekerApplicationPage =
          createPage(),
    ): Promise<void> {
      service =
        new FakeApplicationsService();

      service.response =
        of(response);

      navigate =
        vi.fn()
          .mockResolvedValue(true);

      await TestBed
        .configureTestingModule({
          imports: [
            JobSeekerApplicationsPage,
          ],
          providers: [
            {
              provide:
                JobSeekerApplicationsService,
              useValue:
                service,
            },
            {
              provide:
                Router,
              useValue: {
                navigate,
              },
            },
          ],
        })
        .compileComponents();

      fixture =
        TestBed.createComponent(
          JobSeekerApplicationsPage,
        );

      fixture.detectChanges();

      await fixture.whenStable();

      fixture.detectChanges();
    }

    afterEach(() => {
      vi.restoreAllMocks();
    });

    it('loads the authenticated Job Seeker application page', async () => {
      await createComponent();

      expect(
        service.queries,
      ).toEqual([
        {
          status: undefined,
          page: 1,
          pageSize: 20,
        },
      ]);

      const text =
        fixture.nativeElement
          .textContent as string;

      expect(text)
        .toContain(
          'Backend Developer',
        );

      expect(text)
        .toContain(
          'HireSync Employer',
        );

      expect(text)
        .toContain(
          'Under review',
        );
    });

    it('applies the selected status filter from page one', async () => {
      await createComponent();

      fixture.componentInstance
        .statusControl
        .setValue(
          ApplicationStatus.Shortlisted,
        );

      fixture.componentInstance
        .applyStatusFilter();

      expect(
        service.queries.at(-1),
      ).toEqual({
        status:
          ApplicationStatus.Shortlisted,
        page: 1,
        pageSize: 20,
      });
    });

    it('renders empty state with vacancy discovery action and no withdraw action', async () => {
      await createComponent({
        items: [],
        page: 1,
        pageSize: 20,
        totalCount: 0,
      });

      const text =
        fixture.nativeElement
          .textContent as string;

      expect(text)
        .toContain(
          'No applications found',
        );

      expect(text)
        .toContain(
          'Browse vacancies',
        );

      expect(text)
        .not.toContain(
          'Withdraw',
        );
    });

    it('opens the canonical vacancy detail route', async () => {
      await createComponent();

      fixture.componentInstance
        .openVacancy(
          '22222222-2222-2222-2222-222222222222',
        );

      expect(navigate)
        .toHaveBeenCalledWith([
          '/seeker/vacancies',
          '22222222-2222-2222-2222-222222222222',
        ]);
    });

    it('requests the next page without changing application data locally', async () => {
      await createComponent({
        ...createPage(),
        page: 1,
        pageSize: 20,
        totalCount: 40,
      });

      fixture.componentInstance
        .nextPage();

      expect(
        service.queries.at(-1),
      ).toEqual({
        status: undefined,
        page: 2,
        pageSize: 20,
      });
    });
  },
);

class FakeApplicationsService {
  readonly queries:
    JobSeekerApplicationQuery[] =
      [];

  response:
    Observable<JobSeekerApplicationPage> =
      of(createPage());

  getOwnApplications(
    query:
      JobSeekerApplicationQuery,
  ): Observable<JobSeekerApplicationPage> {
    this.queries.push({
      ...query,
    });

    return this.response;
  }
}

function createPage():
  JobSeekerApplicationPage {
  return {
    items: [
      {
        applicationId:
          '11111111-1111-1111-1111-111111111111',
        vacancyId:
          '22222222-2222-2222-2222-222222222222',
        vacancyTitle:
          'Backend Developer',
        companyName:
          'HireSync Employer',
        vacancyLocation:
          'Colombo',
        vacancyStatus:
          VacancyStatus.Open,
        status:
          ApplicationStatus.UnderReview,
        appliedAtUtc:
          '2026-09-12T10:00:00Z',
        updatedAtUtc:
          '2026-09-12T11:00:00Z',
      },
    ],
    page: 1,
    pageSize: 20,
    totalCount: 1,
  };
}
