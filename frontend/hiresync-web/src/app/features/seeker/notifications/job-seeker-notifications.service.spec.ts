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
  NotificationPage,
  NotificationType,
} from './job-seeker-notifications.models';
import {
  JobSeekerNotificationsService,
} from './job-seeker-notifications.service';

describe(
  'JobSeekerNotificationsService',
  () => {
    let service:
      JobSeekerNotificationsService;

    let httpTesting:
      HttpTestingController;

    const page:
      NotificationPage = {
        items: [
          {
            id:
              '11111111-1111-1111-1111-111111111111',
            type:
              NotificationType.ApplicationStatusChanged,
            title:
              'Application status changed',
            message:
              'Your application status has changed.',
            jobApplicationId:
              '22222222-2222-2222-2222-222222222222',
            isRead:
              false,
            createdAtUtc:
              '2026-09-12T10:00:00Z',
            readAtUtc:
              null,
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
          JobSeekerNotificationsService,
        ],
      });

      service =
        TestBed.inject(
          JobSeekerNotificationsService,
        );

      httpTesting =
        TestBed.inject(
          HttpTestingController,
        );
    });

    afterEach(() => {
      httpTesting.verify();
    });

    it('loads own notifications with bounded paging parameters', () => {
      service
        .getOwnNotifications({
          page: 2,
          pageSize: 20,
        })
        .subscribe();

      const request =
        httpTesting.expectOne(
          (candidate) =>
            candidate.url ===
              '/api/v1/notifications' &&
            candidate.params.get('page') === '2' &&
            candidate.params.get('pageSize') === '20',
        );

      expect(
        request.request.method,
      ).toBe('GET');

      request.flush(page);
    });

    it('marks one notification read through the canonical route', () => {
      const id =
        '11111111-1111-1111-1111-111111111111';

      service
        .markRead(id)
        .subscribe();

      const request =
        httpTesting.expectOne(
          `/api/v1/notifications/${id}/read`,
        );

      expect(
        request.request.method,
      ).toBe('PATCH');

      request.flush(null);
    });

    it('marks all own notifications read through the canonical route', () => {
      service
        .markAllRead()
        .subscribe();

      const request =
        httpTesting.expectOne(
          '/api/v1/notifications/read-all',
        );

      expect(
        request.request.method,
      ).toBe('PATCH');

      request.flush(null);
    });
  },
);
