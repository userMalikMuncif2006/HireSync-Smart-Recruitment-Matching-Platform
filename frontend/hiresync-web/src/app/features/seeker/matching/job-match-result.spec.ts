import { HttpErrorResponse } from '@angular/common/http';
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
  ApplicationCreated,
  PublicVacancyDetail,
} from './job-match.models';
import { JobMatchResult } from './job-match-result';
import { JobMatchService } from './job-match.service';

describe('JobMatchResult', () => {
  let fixture:
    ComponentFixture<JobMatchResult>;

  let service:
    FakeJobMatchService;

  beforeEach(async () => {
    service =
      new FakeJobMatchService();

    await TestBed
      .configureTestingModule({
        imports: [JobMatchResult],
        providers: [
          {
            provide: JobMatchService,
            useValue: service,
          },
        ],
      })
      .compileComponents();

    fixture =
      TestBed.createComponent(
        JobMatchResult,
      );
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders vacancy and backend scores unchanged', async () => {
    await loadDetail();

    const text =
      fixture.nativeElement
        .textContent as string;

    expect(text)
      .toContain('Backend Developer');

    expect(text)
      .toContain('HireSync Employer');

    expect(text)
      .toContain('87.5%');

    expect(text)
      .toContain('37.5 / 50');

    expect(text)
      .toContain('25 / 25');

    expect(text)
      .toContain('15 / 15');

    expect(text)
      .toContain('10 / 10');

    expect(text)
      .toContain('Apply now');
  });

  it('renders ProfileIncomplete as a domain state', async () => {
    service.response =
      of({
        ...createDetail(),
        matchStatus:
          'ProfileIncomplete',
        match: null,
        canApply: false,
        missingProfileFields: [
          'Skills',
          'PreferredLocation',
        ],
      });

    await loadDetail();

    const text =
      fixture.nativeElement
        .textContent as string;

    expect(text)
      .toContain(
        'Complete your structured profile before applying.',
      );

    expect(text)
      .toContain('Skills');

    expect(text)
      .toContain('PreferredLocation');
  });

  it('renders already-applied state', async () => {
    service.response =
      of({
        ...createDetail(),
        canApply: false,
        hasApplied: true,
      });

    await loadDetail();

    const text =
      fixture.nativeElement
        .textContent as string;

    expect(text)
      .toContain(
        'Already applied to this vacancy.',
      );

    expect(
      fixture.nativeElement.querySelector(
        '.apply-button',
      ),
    ).toBeNull();
  });

  it('renders load failure', async () => {
    service.response =
      throwError(
        () => new Error('Request failed'),
      );

    await loadDetail();

    const alert =
      fixture.nativeElement
        .querySelector(
          '[role="alert"]',
        ) as HTMLElement | null;

    expect(alert)
      .not.toBeNull();

    expect(alert?.textContent)
      .toContain(
        'The vacancy detail could not be loaded.',
      );
  });

  it('submits an application and reconciles to Applied', async () => {
    vi.spyOn(
      window,
      'confirm',
    ).mockReturnValue(true);

    service.applyResponse =
      of(createApplication());

    await loadDetail();

    const button =
      fixture.nativeElement.querySelector(
        '.apply-button',
      ) as HTMLButtonElement;

    button.click();

    fixture.detectChanges();

    expect(service.lastAppliedVacancyId)
      .toBe(createDetail().id);

    expect(
      fixture.componentInstance
        .result()
        ?.hasApplied,
    ).toBe(true);

    expect(
      fixture.componentInstance
        .result()
        ?.canApply,
    ).toBe(false);

    expect(
      fixture.nativeElement
        .textContent as string,
    ).toContain(
      'Application submitted successfully.',
    );
  });

  it('reconciles duplicate application conflict to Applied', async () => {
    vi.spyOn(
      window,
      'confirm',
    ).mockReturnValue(true);

    service.applyResponse =
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: {
              code:
                'DUPLICATE_APPLICATION',
            },
          }),
      );

    await loadDetail();

    (
      fixture.nativeElement.querySelector(
        '.apply-button',
      ) as HTMLButtonElement
    ).click();

    fixture.detectChanges();

    expect(
      fixture.componentInstance
        .result()
        ?.hasApplied,
    ).toBe(true);

    expect(
      fixture.componentInstance
        .result()
        ?.canApply,
    ).toBe(false);

    expect(
      fixture.nativeElement
        .textContent as string,
    ).toContain(
      'You have already applied to this vacancy.',
    );
  });

  it('disables applying after vacancy-closed conflict', async () => {
    vi.spyOn(
      window,
      'confirm',
    ).mockReturnValue(true);

    service.applyResponse =
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: {
              code:
                'VACANCY_CLOSED',
            },
          }),
      );

    await loadDetail();

    (
      fixture.nativeElement.querySelector(
        '.apply-button',
      ) as HTMLButtonElement
    ).click();

    fixture.detectChanges();

    expect(
      fixture.componentInstance
        .result()
        ?.hasApplied,
    ).toBe(false);

    expect(
      fixture.componentInstance
        .result()
        ?.canApply,
    ).toBe(false);

    expect(
      fixture.nativeElement
        .textContent as string,
    ).toContain(
      'This vacancy closed before your application could be submitted.',
    );
  });

  async function loadDetail(): Promise<void> {
    fixture.componentRef.setInput(
      'vacancyId',
      createDetail().id,
    );

    fixture.detectChanges();

    await fixture.whenStable();

    fixture.detectChanges();
  }
});

function createDetail():
  PublicVacancyDetail {
  return {
    id:
      '33333333-3333-3333-3333-333333333333',
    title:
      'Backend Developer',
    description:
      'Build secure services.',
    companyName:
      'HireSync Employer',
    companyDescription:
      'Recruitment company',
    companyWebsite:
      null,
    companyLocation:
      'Colombo',
    location:
      'Colombo',
    minimumExperienceMonths:
      12,
    requiredEducationLevel:
      2,
    publishedAtUtc:
      '2026-09-12T09:00:00Z',
    requiredSkills: [
      {
        id:
          '11111111-1111-1111-1111-111111111111',
        name:
          'C#',
      },
    ],
    matchStatus:
      'Ready',
    match: {
      totalScore:
        87.5,
      skillsScore:
        37.5,
      experienceScore:
        25,
      educationScore:
        15,
      locationScore:
        10,
      matchedSkills: [],
      missingSkills: [],
    },
    missingProfileFields: [],
    canApply:
      true,
    hasApplied:
      false,
    computedAtUtc:
      '2026-09-12T12:30:00Z',
  };
}

function createApplication():
  ApplicationCreated {
  return {
    id:
      '44444444-4444-4444-4444-444444444444',
    vacancyId:
      createDetail().id,
    status: 1,
    appliedAtUtc:
      '2026-09-12T12:00:00Z',
    updatedAtUtc:
      '2026-09-12T12:00:00Z',
  };
}

class FakeJobMatchService {
  response:
    Observable<PublicVacancyDetail> =
      of(createDetail());

  applyResponse:
    Observable<ApplicationCreated> =
      of(createApplication());

  lastAppliedVacancyId:
    string | null = null;

  getVacancyDetail(
    _vacancyId: string,
  ): Observable<PublicVacancyDetail> {
    return this.response;
  }

  applyToVacancy(
    vacancyId: string,
  ): Observable<ApplicationCreated> {
    this.lastAppliedVacancyId =
      vacancyId;

    return this.applyResponse;
  }
}