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
  CreateVacancyPayload,
  EmployerVacancy,
  SkillSummary,
  UpdateVacancyPayload,
} from './employer-vacancy.models';
import { EmployerVacancyFormPage } from './employer-vacancy-form-page';
import { EmployerVacancyService } from './employer-vacancy.service';

describe('EmployerVacancyFormPage', () => {
  let fixture:
    ComponentFixture<EmployerVacancyFormPage>;

  let service:
    FakeEmployerVacancyService;

  let navigate:
    ReturnType<typeof vi.fn>;

  async function createComponent(
    vacancyId:
      string | null = null,
    vacancy:
      EmployerVacancy =
        createVacancy(),
  ): Promise<void> {
    service =
      new FakeEmployerVacancyService();

    service.vacancyResponse =
      of(vacancy);

    navigate =
      vi.fn().mockResolvedValue(true);

    await TestBed
      .configureTestingModule({
        imports: [
          EmployerVacancyFormPage,
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
                  convertToParamMap(
                    vacancyId
                      ? {
                          vacancyId,
                        }
                      : {},
                  ),
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
        EmployerVacancyFormPage,
      );

    fixture.detectChanges();

    await fixture.whenStable();

    fixture.detectChanges();
  }

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('loads the canonical C2 skill catalogue in create mode', async () => {
    await createComponent();

    expect(service.skillQueries)
      .toEqual([
        '',
      ]);

    const text =
      fixture.nativeElement
        .textContent as string;

    expect(text)
      .toContain('Create vacancy');

    expect(text)
      .toContain('C#');

    expect(text)
      .toContain('Angular');
  });

  it('creates using selected canonical skill ids and trimmed text fields', async () => {
    await createComponent();

    fixture.componentInstance
      .form
      .setValue({
        title:
          '  Backend Developer  ',
        description:
          '  Build secure and reliable backend services.  ',
        location:
          '  Colombo  ',
        minimumExperienceMonths:
          24,
        requiredEducationLevel:
          5,
      });

    fixture.componentInstance
      .toggleSkill({
        id:
          'skill-1',
        name:
          'C#',
      });

    fixture.componentInstance
      .save();

    expect(service.created)
      .toEqual([
        {
          title:
            'Backend Developer',
          description:
            'Build secure and reliable backend services.',
          location:
            'Colombo',
          minimumExperienceMonths:
            24,
          requiredEducationLevel:
            5,
          requiredSkillIds: [
            'skill-1',
          ],
        },
      ]);

    expect(navigate)
      .toHaveBeenCalledWith([
        '/employer/vacancies',
      ]);
  });

  it('hydrates edit mode and preserves the server row version on update', async () => {
    await createComponent(
      'vacancy-1',
    );

    expect(service.loadedVacancyIds)
      .toEqual([
        'vacancy-1',
      ]);

    expect(
      fixture.componentInstance
        .form
        .getRawValue(),
    ).toEqual({
      title:
        'Backend Developer',
      description:
        'Build and maintain backend services.',
      location:
        'Colombo',
      minimumExperienceMonths:
        24,
      requiredEducationLevel:
        5,
    });

    expect(
      fixture.componentInstance
        .selectedSkills(),
    ).toEqual([
      {
        id:
          'skill-1',
        name:
          'C#',
      },
    ]);

    fixture.componentInstance
      .form.controls.title
      .setValue(
        'Senior Backend Developer',
      );

    fixture.componentInstance
      .save();

    expect(service.updated)
      .toEqual([
        {
          vacancyId:
            'vacancy-1',
          payload: {
            title:
              'Senior Backend Developer',
            description:
              'Build and maintain backend services.',
            location:
              'Colombo',
            minimumExperienceMonths:
              24,
            requiredEducationLevel:
              5,
            requiredSkillIds: [
              'skill-1',
            ],
            rowVersion:
              'AQIDBA==',
          },
        },
      ]);
  });

  it('keeps a Closed vacancy read-only and does not send an update', async () => {
    await createComponent(
      'vacancy-1',
      {
        ...createVacancy(),
        status:
          2,
        closedAtUtc:
          '2026-09-12T12:00:00Z',
      },
    );

    expect(
      fixture.componentInstance
        .isClosed(),
    ).toBe(true);

    expect(
      fixture.componentInstance
        .form.disabled,
    ).toBe(true);

    fixture.componentInstance
      .save();

    expect(service.updated)
      .toEqual([]);

    expect(
      fixture.componentInstance
        .errorMessage(),
    ).toContain(
      'Closed vacancy is read-only',
    );
  });

  it('requires at least one canonical required skill before saving', async () => {
    await createComponent();

    fixture.componentInstance
      .form
      .setValue({
        title:
          'Backend Developer',
        description:
          'Build secure and reliable backend services.',
        location:
          'Colombo',
        minimumExperienceMonths:
          24,
        requiredEducationLevel:
          null,
      });

    fixture.componentInstance
      .save();

    expect(service.created)
      .toEqual([]);

    expect(
      fixture.componentInstance
        .skillError(),
    ).toContain(
      'at least one',
    );
  });
});

function createVacancy():
  EmployerVacancy {
  return {
    id:
      'vacancy-1',
    title:
      'Backend Developer',
    description:
      'Build and maintain backend services.',
    location:
      'Colombo',
    minimumExperienceMonths:
      24,
    requiredEducationLevel:
      5,
    status:
      1,
    publishedAtUtc:
      '2026-09-12T08:00:00Z',
    updatedAtUtc:
      '2026-09-12T09:00:00Z',
    closedAtUtc:
      null,
    requiredSkills: [
      {
        id:
          'skill-1',
        name:
          'C#',
      },
    ],
    rowVersion:
      'AQIDBA==',
  };
}

class FakeEmployerVacancyService {
  readonly skills:
    SkillSummary[] = [
      {
        id:
          'skill-1',
        name:
          'C#',
      },
      {
        id:
          'skill-2',
        name:
          'Angular',
      },
    ];

  vacancyResponse:
    Observable<EmployerVacancy> =
      of(createVacancy());

  readonly skillQueries:
    string[] = [];

  readonly loadedVacancyIds:
    string[] = [];

  readonly created:
    CreateVacancyPayload[] = [];

  readonly updated:
    Array<{
      vacancyId: string;
      payload:
        UpdateVacancyPayload;
    }> = [];

  getSkills(
    query?: string,
  ): Observable<SkillSummary[]> {
    this.skillQueries.push(
      query ?? '',
    );

    return of(this.skills);
  }

  getVacancy(
    vacancyId: string,
  ): Observable<EmployerVacancy> {
    this.loadedVacancyIds.push(
      vacancyId,
    );

    return this.vacancyResponse;
  }

  createVacancy(
    payload:
      CreateVacancyPayload,
  ): Observable<EmployerVacancy> {
    this.created.push({
      ...payload,
      requiredSkillIds: [
        ...payload.requiredSkillIds,
      ],
    });

    return of(
      createVacancy(),
    );
  }

  updateVacancy(
    vacancyId: string,
    payload:
      UpdateVacancyPayload,
  ): Observable<EmployerVacancy> {
    this.updated.push({
      vacancyId,
      payload: {
        ...payload,
        requiredSkillIds: [
          ...payload.requiredSkillIds,
        ],
      },
    });

    return of(
      createVacancy(),
    );
  }
}