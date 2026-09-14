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
  ContactRequestStatus,
  RespondContactRequestPayload,
} from './job-seeker-contact-requests.models';
import {
  JobSeekerContactRequestsService,
} from './job-seeker-contact-requests.service';

describe(
  'JobSeekerContactRequestsService',
  () => {
    let service:
      JobSeekerContactRequestsService;

    let httpTesting:
      HttpTestingController;

    beforeEach(() => {
      TestBed.configureTestingModule({
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          JobSeekerContactRequestsService,
        ],
      });

      service =
        TestBed.inject(
          JobSeekerContactRequestsService,
        );

      httpTesting =
        TestBed.inject(
          HttpTestingController,
        );
    });

    afterEach(() => {
      httpTesting.verify();
    });

    it('loads the authenticated Job Seeker contact requests', () => {
      service
        .getOwnContactRequests()
        .subscribe();

      const request =
        httpTesting.expectOne(
          '/api/v1/job-seeker/contact-requests',
        );

      expect(request.request.method)
        .toBe('GET');

      request.flush([
        {
          id:
            '11111111-1111-1111-1111-111111111111',
          jobApplicationId:
            '22222222-2222-2222-2222-222222222222',
          vacancyId:
            '33333333-3333-3333-3333-333333333333',
          vacancyTitle:
            'Backend Developer',
          employerCompanyName:
            'HireSync Employer',
          status:
            ContactRequestStatus.Pending,
          requestedAtUtc:
            '2026-09-13T03:00:00Z',
          respondedAtUtc:
            null,
          rowVersion:
            'AQIDBA==',
        },
      ]);
    });

    it('responds to a contact request using status and row version', () => {
      const payload:
        RespondContactRequestPayload = {
          status:
            ContactRequestStatus.Accepted,
          rowVersion:
            'AQIDBA==',
        };

      service
        .respondToContactRequest(
          '11111111-1111-1111-1111-111111111111',
          payload,
        )
        .subscribe();

      const request =
        httpTesting.expectOne(
          '/api/v1/job-seeker/contact-requests/11111111-1111-1111-1111-111111111111/status',
        );

      expect(request.request.method)
        .toBe('PATCH');

      expect(request.request.body)
        .toEqual(payload);

      request.flush({
        id:
          '11111111-1111-1111-1111-111111111111',
        jobApplicationId:
          '22222222-2222-2222-2222-222222222222',
        status:
          ContactRequestStatus.Accepted,
        requestedAtUtc:
          '2026-09-13T03:00:00Z',
        respondedAtUtc:
          '2026-09-13T03:05:00Z',
        rowVersion:
          'BQYHCA==',
      });
    });
  },
);
