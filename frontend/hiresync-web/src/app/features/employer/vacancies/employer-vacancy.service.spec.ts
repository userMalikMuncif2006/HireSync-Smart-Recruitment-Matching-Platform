import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

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
      vacancyId: 'vacancy-1',
      vacancyTitle: 'Backend Developer',
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
  });
});