import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import {
  CreateVacancyPayload,
  EmployerVacancy,
  UpdateVacancyPayload,
} from './employer-vacancy.models';
import { EmployerVacancyService } from './employer-vacancy.service';

describe('EmployerVacancyService', () => {
  let service: EmployerVacancyService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        EmployerVacancyService,
      ],
    });

    service =
      TestBed.inject(EmployerVacancyService);

    httpTesting =
      TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('loads Employer vacancies with status and paging', () => {
    service
      .getVacancies(
        1,
        2,
        20,
      )
      .subscribe();

    const request =
      httpTesting.expectOne(
        (candidate) =>
          candidate.url ===
          '/api/v1/employer/vacancies',
      );

    expect(request.request.method)
      .toBe('GET');

    expect(
      request.request.params.get('status'),
    ).toBe('1');

    expect(
      request.request.params.get('page'),
    ).toBe('2');

    expect(
      request.request.params.get('pageSize'),
    ).toBe('20');

    request.flush({
      items: [],
      page: 2,
      pageSize: 20,
      totalCount: 0,
    });
  });

  it('loads one Employer-owned vacancy for editing', () => {
    service
      .getVacancy(
        'vacancy-1',
      )
      .subscribe();

    const request =
      httpTesting.expectOne(
        '/api/v1/employer/vacancies/vacancy-1',
      );

    expect(request.request.method)
      .toBe('GET');

    request.flush(
      createVacancy(),
    );
  });

  it('creates a vacancy using canonical required skill ids', () => {
    const payload:
      CreateVacancyPayload = {
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
        requiredSkillIds: [
          'skill-1',
          'skill-2',
        ],
      };

    service
      .createVacancy(payload)
      .subscribe();

    const request =
      httpTesting.expectOne(
        '/api/v1/employer/vacancies',
      );

    expect(request.request.method)
      .toBe('POST');

    expect(request.request.body)
      .toEqual(payload);

    request.flush(
      createVacancy(),
    );
  });

  it('updates a vacancy with its current row version', () => {
    const payload:
      UpdateVacancyPayload = {
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
        requiredSkillIds: [
          'skill-1',
        ],
        rowVersion:
          'AQIDBA==',
      };

    service
      .updateVacancy(
        'vacancy-1',
        payload,
      )
      .subscribe();

    const request =
      httpTesting.expectOne(
        '/api/v1/employer/vacancies/vacancy-1',
      );

    expect(request.request.method)
      .toBe('PUT');

    expect(request.request.body)
      .toEqual(payload);

    request.flush(
      createVacancy(),
    );
  });

  it('uses the canonical C2 skill lookup and trims only the query boundary', () => {
    service
      .getSkills(
        '  C#  ',
      )
      .subscribe();

    const request =
      httpTesting.expectOne(
        (candidate) =>
          candidate.url ===
          '/api/v1/skills',
      );

    expect(request.request.method)
      .toBe('GET');

    expect(
      request.request.params.get('query'),
    ).toBe('C#');

    request.flush([
      {
        id:
          'skill-1',
        name:
          'C#',
      },
    ]);
  });

  it('omits the C2 query parameter when requesting all skills', () => {
    service
      .getSkills('   ')
      .subscribe();

    const request =
      httpTesting.expectOne(
        (candidate) =>
          candidate.url ===
          '/api/v1/skills',
      );

    expect(
      request.request.params.has('query'),
    ).toBe(false);

    request.flush([]);
  });

  it('closes a vacancy using its concurrency row version', () => {
    service
      .closeVacancy(
        'vacancy-1',
        'AQIDBA==',
      )
      .subscribe();

    const request =
      httpTesting.expectOne(
        '/api/v1/employer/vacancies/vacancy-1/status',
      );

    expect(request.request.method)
      .toBe('PATCH');

    expect(request.request.body)
      .toEqual({
        status: 2,
        rowVersion: 'AQIDBA==',
      });

    request.flush({
      id: 'vacancy-1',
      status: 2,
      closedAtUtc:
        '2026-09-12T12:00:00Z',
      rowVersion: 'BQYHCA==',
    });
  });

  it('creates a contact request for an application', () => {
    service
      .createContactRequest(
        'application-1',
      )
      .subscribe();

    const request =
      httpTesting.expectOne(
        '/api/v1/employer/applications/application-1/contact-requests',
      );

    expect(request.request.method)
      .toBe('POST');

    expect(request.request.body)
      .toBeNull();

    request.flush({
      id: 'contact-1',
      jobApplicationId: 'application-1',
      status: 1,
      requestedAtUtc:
        '2026-09-13T03:00:00Z',
      respondedAtUtc: null,
      rowVersion: 'AQIDBA==',
    });
  });

  it('updates an application status using its concurrency row version', () => {
    const payload:
      UpdateApplicationStatusPayload = {
        status: 3,
        rowVersion: 'AQIDBA==',
      };

    service
      .updateApplicationStatus(
        'application-1',
        payload,
      )
      .subscribe();

    const request =
      httpTesting.expectOne(
        '/api/v1/employer/applications/application-1/status',
      );

    expect(request.request.method)
      .toBe('PATCH');

    expect(request.request.body)
      .toEqual(payload);

    request.flush({
      id: 'application-1',
      status: 3,
      updatedAtUtc:
        '2026-09-13T00:00:00Z',
      rowVersion: 'BQYHCA==',
    });
  });
  it('loads ranked applicants with application-status filtering', () => {
    service
      .getRankedApplicants(
        'vacancy-1',
        3,
        1,
        20,
      )
      .subscribe();

    const request =
      httpTesting.expectOne(
        (candidate) =>
          candidate.url ===
          '/api/v1/employer/vacancies/vacancy-1/applicants',
      );

    expect(request.request.method)
      .toBe('GET');

    expect(
      request.request.params.get('status'),
    ).toBe('3');

    expect(
      request.request.params.get('page'),
    ).toBe('1');

    expect(
      request.request.params.get('pageSize'),
    ).toBe('20');

    request.flush({
      vacancyId:
        'vacancy-1',
      vacancyTitle:
        'Backend Developer',
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
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
