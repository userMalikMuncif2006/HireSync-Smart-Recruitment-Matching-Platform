import {
  provideHttpClient,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import {
  TestBed,
} from '@angular/core/testing';

import {
  ApplicationStatus,
  JobSeekerApplicationPage,
  VacancyStatus,
} from './job-seeker-applications.models';
import {
  JobSeekerApplicationsService,
} from './job-seeker-applications.service';

describe(
  'JobSeekerApplicationsService',
  () => {
    let service:
      JobSeekerApplicationsService;

    let httpTesting:
      HttpTestingController;

    const page:
      JobSeekerApplicationPage = {
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

    beforeEach(() => {
      TestBed.configureTestingModule({
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          JobSeekerApplicationsService,
        ],
      });

      service =
        TestBed.inject(
          JobSeekerApplicationsService,
        );

      httpTesting =
        TestBed.inject(
          HttpTestingController,
        );
    });

    afterEach(() => {
      httpTesting.verify();
    });

    it('loads own applications with bounded paging parameters', () => {
      service
        .getOwnApplications({
          page: 2,
          pageSize: 20,
        })
        .subscribe();

      const request =
        httpTesting.expectOne(
          (candidate) =>
            candidate.url ===
              '/api/v1/job-seeker/applications' &&
            candidate.params.get('page') === '2' &&
            candidate.params.get('pageSize') === '20' &&
            !candidate.params.has('status'),
        );

      expect(
        request.request.method,
      ).toBe('GET');

      request.flush(page);
    });

    it('sends the canonical numeric application status filter', () => {
      service
        .getOwnApplications({
          status:
            ApplicationStatus.Shortlisted,
          page: 1,
          pageSize: 20,
        })
        .subscribe();

      const request =
        httpTesting.expectOne(
          (candidate) =>
            candidate.url ===
              '/api/v1/job-seeker/applications' &&
            candidate.params.get('status') === '3',
        );

      expect(
        request.request.method,
      ).toBe('GET');

      request.flush(page);
    });
  },
);
