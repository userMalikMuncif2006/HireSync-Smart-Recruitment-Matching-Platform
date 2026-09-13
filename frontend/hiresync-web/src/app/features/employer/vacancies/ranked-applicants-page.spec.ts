import { HttpErrorResponse } from '@angular/common/http';
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
  Subject,
  throwError,
} from 'rxjs';

import {
  ApplicationStatus,
  ApplicationStatusResult,
  ContactRequestResult,
  ContactRequestStatus,
  RankedApplicantPage,
  UpdateApplicationStatusPayload,
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

  it('updates status with the current row version and re-fetches authoritative applicant data', async () => {
    await createComponent();

    service.updateResponse =
      of({
        id:
          'application-1',
        status:
          4,
        updatedAtUtc:
          '2026-09-13T02:00:00Z',
        rowVersion:
          'BQYHCA==',
      });

    service.response =
      of(
        createApplicantPage(
          4,
          'BQYHCA==',
        ),
      );

    const select =
      fixture.nativeElement
        .querySelector(
          '.status-workflow select',
        ) as HTMLSelectElement;

    const button =
      fixture.nativeElement
        .querySelector(
          '.status-workflow button',
        ) as HTMLButtonElement;

    select.value = '4';

    select.dispatchEvent(
      new Event('change'),
    );

    fixture.detectChanges();

    expect(button.disabled)
      .toBe(false);

    button.click();

    fixture.detectChanges();

    expect(service.updates)
      .toEqual([
        {
          applicationId:
            'application-1',
          payload: {
            status:
              4,
            rowVersion:
              'AQIDBA==',
          },
        },
      ]);

    expect(service.queries)
      .toHaveLength(2);

    expect(service.queries.at(-1))
      .toEqual({
        vacancyId:
          'vacancy-1',
        status:
          null,
        page:
          1,
        pageSize:
          20,
      });

    expect(
      fixture.componentInstance
        .result()
        ?.items[0]
        .status,
    ).toBe(4);

    expect(
      fixture.componentInstance
        .result()
        ?.items[0]
        .rowVersion,
    ).toBe('BQYHCA==');

    expect(
      fixture.componentInstance
        .selectedApplicationStatus(
          fixture.componentInstance
            .result()!
            .items[0],
        ),
    ).toBe(4);
  });

  it('creates a contact request and re-fetches authoritative applicant data', async () => {
    await createComponent(
      createApplicantPage(
        3,
        'AQIDBA==',
        null,
      ),
    );

    service.createContactResponse =
      of({
        id:
          'contact-1',
        jobApplicationId:
          'application-1',
        status:
          1,
        requestedAtUtc:
          '2026-09-13T03:00:00Z',
        respondedAtUtc:
          null,
        rowVersion:
          'AQIDBA==',
      });

    service.response =
      of(
        createApplicantPage(
          3,
          'AQIDBA==',
          1,
        ),
      );

    const button =
      fixture.nativeElement
        .querySelector(
          '.contact-workflow button',
        ) as HTMLButtonElement;

    expect(button)
      .not.toBeNull();

    button.click();

    fixture.detectChanges();

    expect(service.contactCreates)
      .toEqual([
        'application-1',
      ]);

    expect(service.queries)
      .toHaveLength(2);

    expect(
      fixture.componentInstance
        .result()
        ?.items[0]
        .contactRequestStatus,
    ).toBe(1);

    expect(
      fixture.componentInstance
        .contactSuccessMessage(),
    ).toBe(
      'Contact request created successfully.',
    );

    expect(
      fixture.nativeElement
        .querySelector(
          '.contact-workflow button',
        ),
    ).toBeNull();
  });

  it('prevents duplicate contact submission while creation is in progress', async () => {
    await createComponent(
      createApplicantPage(
        3,
        'AQIDBA==',
        null,
      ),
    );

    const pending =
      new Subject<ContactRequestResult>();

    service.createContactResponse =
      pending;

    const button =
      fixture.nativeElement
        .querySelector(
          '.contact-workflow button',
        ) as HTMLButtonElement;

    button.click();
    fixture.detectChanges();

    expect(service.contactCreates)
      .toEqual([
        'application-1',
      ]);

    expect(button.disabled)
      .toBe(true);

    fixture.componentInstance
      .createContactRequest(
        fixture.componentInstance
          .result()!
          .items[0],
      );

    expect(service.contactCreates)
      .toHaveLength(1);

    pending.complete();
  });

  it('surfaces backend ProblemDetails when contact creation fails', async () => {
    await createComponent(
      createApplicantPage(
        3,
        'AQIDBA==',
        null,
      ),
    );

    service.createContactResponse =
      throwError(
        () =>
          new HttpErrorResponse({
            status:
              409,
            error: {
              detail:
                'A contact request already exists for this application.',
            },
          }),
      );

    fixture.componentInstance
      .createContactRequest(
        fixture.componentInstance
          .result()!
          .items[0],
      );

    fixture.detectChanges();

    expect(
      fixture.componentInstance
        .contactErrorMessage(),
    ).toBe(
      'A contact request already exists for this application.',
    );

    expect(service.queries)
      .toHaveLength(1);
  });

  it('does not offer another contact request when one already exists', async () => {
    await createComponent(
      createApplicantPage(
        3,
        'AQIDBA==',
        2,
      ),
    );

    expect(
      fixture.nativeElement
        .querySelector(
          '.contact-workflow button',
        ),
    ).toBeNull();

    expect(
      fixture.nativeElement
        .textContent as string,
    ).toContain(
      'Contact accepted',
    );
  });

  it('surfaces backend ProblemDetails for an invalid transition', async () => {
    await createComponent();

    service.updateResponse =
      throwError(
        () =>
          new HttpErrorResponse({
            status:
              400,
            error: {
              detail:
                'The requested application status transition is not allowed.',
            },
          }),
      );

    const applicant =
      fixture.componentInstance
        .result()!
        .items[0];

    fixture.componentInstance
      .changeSelectedApplicationStatus(
        applicant.applicationId,
        '1',
      );

    fixture.componentInstance
      .updateApplicationStatus(
        applicant,
      );

    fixture.detectChanges();

    expect(
      fixture.componentInstance
        .updateErrorMessage(),
    ).toBe(
      'The requested application status transition is not allowed.',
    );

    expect(service.queries)
      .toHaveLength(1);
  });

  it('shows an understandable fallback for a concurrency conflict', async () => {
    await createComponent();

    service.updateResponse =
      throwError(
        () =>
          new HttpErrorResponse({
            status:
              409,
            error: {},
          }),
      );

    const applicant =
      fixture.componentInstance
        .result()!
        .items[0];

    fixture.componentInstance
      .changeSelectedApplicationStatus(
        applicant.applicationId,
        '4',
      );

    fixture.componentInstance
      .updateApplicationStatus(
        applicant,
      );

    fixture.detectChanges();

    expect(
      fixture.componentInstance
        .updateErrorMessage(),
    ).toContain(
      'application changed',
    );

    expect(service.queries)
      .toHaveLength(1);
  });

  it('prevents status changes for terminal application states', async () => {
    await createComponent(
      createApplicantPage(
        4,
        'BQYHCA==',
      ),
    );

    expect(
      fixture.componentInstance
        .isTerminalStatus(4),
    ).toBe(true);

    expect(
      fixture.componentInstance
        .isTerminalStatus(5),
    ).toBe(true);

    const select =
      fixture.nativeElement
        .querySelector(
          '.status-workflow select',
        ) as HTMLSelectElement;

    const button =
      fixture.nativeElement
        .querySelector(
          '.status-workflow button',
        ) as HTMLButtonElement;

    expect(select.disabled)
      .toBe(true);

    expect(button.disabled)
      .toBe(true);

    expect(
      fixture.nativeElement
        .textContent as string,
    ).toContain(
      'terminal state',
    );
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

function createApplicantPage(
  status:
    ApplicationStatus = 3,
  rowVersion = 'AQIDBA==',
  contactRequestStatus:
    ContactRequestStatus | null = 1,
): RankedApplicantPage {
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
        status,
        appliedAtUtc:
          '2026-09-12T08:00:00Z',
        updatedAtUtc:
          '2026-09-12T09:00:00Z',
        rowVersion,
        contactRequestStatus,
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

  updateResponse:
    Observable<ApplicationStatusResult> =
      of({
        id:
          'application-1',
        status:
          3,
        updatedAtUtc:
          '2026-09-12T09:00:00Z',
        rowVersion:
          'AQIDBA==',
      });

  createContactResponse:
    Observable<ContactRequestResult> =
      of({
        id:
          'contact-1',
        jobApplicationId:
          'application-1',
        status:
          1,
        requestedAtUtc:
          '2026-09-13T03:00:00Z',
        respondedAtUtc:
          null,
        rowVersion:
          'AQIDBA==',
      });

  readonly contactCreates:
    string[] = [];

  readonly queries:
    Array<{
      vacancyId: string;
      status:
        ApplicationStatus | null;
      page: number;
      pageSize: number;
    }> = [];

  readonly updates:
    Array<{
      applicationId: string;
      payload:
        UpdateApplicationStatusPayload;
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

  createContactRequest(
    applicationId: string,
  ): Observable<ContactRequestResult> {
    this.contactCreates.push(
      applicationId,
    );

    return this.createContactResponse;
  }

  updateApplicationStatus(
    applicationId: string,
    payload:
      UpdateApplicationStatusPayload,
  ): Observable<ApplicationStatusResult> {
    this.updates.push({
      applicationId,
      payload,
    });

    return this.updateResponse;
  }
}