import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  ActivatedRoute,
  convertToParamMap,
  Router,
} from '@angular/router';
import {
  Observable,
  of,
} from 'rxjs';

import {
  ApplicationStatus,
  RankedApplicantPage,
} from './employer-vacancy.models';
import { EmployerVacancyService } from './employer-vacancy.service';
import { RankedApplicantsPage } from './ranked-applicants-page';

describe('RankedApplicantsPage', () => {
  let fixture:
    ComponentFixture<RankedApplicantsPage>;

  let service:
    FakeRankedApplicantService;

  let navigate:
    ReturnType<typeof vi.fn>;

  async function createComponent(
    response:
      RankedApplicantPage =
        createApplicantPage(),
  ): Promise<void> {
    service =
      new FakeRankedApplicantService();

    service.response =
      of(response);

    navigate =
      vi.fn().mockResolvedValue(true);

    await TestBed
      .configureTestingModule({
        imports: [
          RankedApplicantsPage,
        ],
        providers: [
          {
            provide:
              EmployerVacancyService,
            useValue:
              service,
          },
          {
            provide:
              ActivatedRoute,
            useValue: {
              snapshot: {
                paramMap:
                  convertToParamMap({
                    vacancyId:
                      'vacancy-1',
                  }),
              },
            },
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
        RankedApplicantsPage,
      );

    fixture.detectChanges();

    await fixture.whenStable();

    fixture.detectChanges();
  }

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders authoritative total and weighted component scores unchanged', async () => {
    await createComponent();

    expect(service.queries)
      .toEqual([
        {
          vacancyId:
            'vacancy-1',
          status:
            null,
          page:
            1,
          pageSize:
            20,
        },
      ]);

    const text =
      fixture.nativeElement
        .textContent as string;

    expect(text)
      .toContain('Candidate One');

    expect(text)
      .toContain('88.25%');

    expect(text)
      .toContain('45.00 / 50');

    expect(text)
      .toContain('20.00 / 25');

    expect(text)
      .toContain('13.25 / 15');

    expect(text)
      .toContain('10.00 / 10');

    expect(text)
      .toContain('CSharp');

    expect(text)
      .toContain('Azure');
  });

  it('applies an application-status filter from page one', async () => {
    await createComponent();

    fixture.componentInstance
      .statusFilter
      .setValue('3');

    fixture.componentInstance
      .applyFilter();

    expect(service.queries.at(-1))
      .toEqual({
        vacancyId:
          'vacancy-1',
        status:
          3,
        page:
          1,
        pageSize:
          20,
      });
  });

  it('returns to Employer vacancies', async () => {
    await createComponent();

    fixture.componentInstance
      .backToVacancies();

    expect(navigate)
      .toHaveBeenCalledWith([
        '/employer/vacancies',
      ]);
  });
});

function createApplicantPage():
  RankedApplicantPage {
  return {
    vacancyId:
      'vacancy-1',
    vacancyTitle:
      'Backend Developer',
    items: [
      {
        rank: 1,
        applicationId:
          'application-1',
        jobSeekerProfileId:
          'profile-1',
        jobSeekerDisplayName:
          'Candidate One',
        status: 3,
        appliedAtUtc:
          '2026-09-12T08:00:00Z',
        updatedAtUtc:
          '2026-09-12T09:00:00Z',
        rowVersion:
          'AQIDBA==',
        contactRequestStatus:
          1,
        match: {
          totalScore:
            88.25,
          skillsScore:
            45,
          experienceScore:
            20,
          educationScore:
            13.25,
          locationScore:
            10,
          matchedSkills: [
            {
              id: 'skill-1',
              name: 'CSharp',
            },
          ],
          missingSkills: [
            {
              id: 'skill-2',
              name: 'Azure',
            },
          ],
        },
      },
    ],
    page: 1,
    pageSize: 20,
    totalCount: 1,
  };
}

class FakeRankedApplicantService {
  response:
    Observable<RankedApplicantPage> =
      of(createApplicantPage());

  readonly queries:
    Array<{
      vacancyId: string;
      status:
        ApplicationStatus | null;
      page: number;
      pageSize: number;
    }> = [];

  getRankedApplicants(
    vacancyId: string,
    status:
      ApplicationStatus | null,
    page: number,
    pageSize: number,
  ): Observable<RankedApplicantPage> {
    this.queries.push({
      vacancyId,
      status,
      page,
      pageSize,
    });

    return this.response;
  }
}