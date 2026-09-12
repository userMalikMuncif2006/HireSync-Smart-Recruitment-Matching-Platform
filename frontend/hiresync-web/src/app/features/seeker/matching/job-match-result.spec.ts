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

  it('renders vacancy and backend scores unchanged', async () => {
    service.response =
      of(createDetail());

    fixture.componentRef.setInput(
      'vacancyId',
      '33333333-3333-3333-3333-333333333333',
    );

    fixture.detectChanges();

    await fixture.whenStable();

    fixture.detectChanges();

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
      .toContain(
        'Your profile and current CV are ready',
      );
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

    fixture.componentRef.setInput(
      'vacancyId',
      '33333333-3333-3333-3333-333333333333',
    );

    fixture.detectChanges();

    await fixture.whenStable();

    fixture.detectChanges();

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

    expect(text)
      .toContain(
        'Complete your structured profile to see your deterministic match score.',
      );
  });

  it('renders already-applied state', async () => {
    service.response =
      of({
        ...createDetail(),
        canApply: false,
        hasApplied: true,
      });

    fixture.componentRef.setInput(
      'vacancyId',
      '33333333-3333-3333-3333-333333333333',
    );

    fixture.detectChanges();

    await fixture.whenStable();

    fixture.detectChanges();

    const text =
      fixture.nativeElement
        .textContent as string;

    expect(text)
      .toContain(
        'Already applied to this vacancy.',
      );
  });

  it('renders load failure', async () => {
    service.response =
      throwError(
        () => new Error('Request failed'),
      );

    fixture.componentRef.setInput(
      'vacancyId',
      '33333333-3333-3333-3333-333333333333',
    );

    fixture.detectChanges();

    await fixture.whenStable();

    fixture.detectChanges();

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
      matchedSkills: [
        {
          id:
            '11111111-1111-1111-1111-111111111111',
          name:
            'C#',
        },
      ],
      missingSkills: [
        {
          id:
            '22222222-2222-2222-2222-222222222222',
          name:
            'Angular',
        },
      ],
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

class FakeJobMatchService {
  response:
    Observable<PublicVacancyDetail> =
      of(createDetail());

  getVacancyDetail(
    _vacancyId: string,
  ): Observable<PublicVacancyDetail> {
    return this.response;
  }
}