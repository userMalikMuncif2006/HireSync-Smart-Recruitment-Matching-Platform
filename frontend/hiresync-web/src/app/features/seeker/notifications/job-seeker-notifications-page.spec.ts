import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  Router,
} from '@angular/router';
import {
  Observable,
  of,
} from 'rxjs';

import {
  NotificationPage,
  NotificationQuery,
  NotificationType,
} from './job-seeker-notifications.models';
import {
  JobSeekerNotificationsPage,
} from './job-seeker-notifications-page';
import {
  JobSeekerNotificationsService,
} from './job-seeker-notifications.service';

describe(
  'JobSeekerNotificationsPage',
  () => {
    let fixture:
      ComponentFixture<
        JobSeekerNotificationsPage
      >;

    let service:
      FakeNotificationsService;

    let navigate:
      ReturnType<typeof vi.fn>;

    async function createComponent(
      response:
        NotificationPage =
          createPage(),
    ): Promise<void> {
      service =
        new FakeNotificationsService();

      service.response =
        of(response);

      navigate =
        vi.fn()
          .mockResolvedValue(true);

      await TestBed
        .configureTestingModule({
          imports: [
            JobSeekerNotificationsPage,
          ],
          providers: [
            {
              provide:
                JobSeekerNotificationsService,
              useValue:
                service,
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
          JobSeekerNotificationsPage,
        );

      fixture.detectChanges();

      await fixture.whenStable();

      fixture.detectChanges();
    }

    afterEach(() => {
      vi.restoreAllMocks();
    });

    it('renders unread and read application-status notifications', async () => {
      await createComponent();

      const text =
        fixture.nativeElement
          .textContent as string;

      expect(text)
        .toContain(
          'Application status changed',
        );

      expect(text)
        .toContain(
          'Unread',
        );

      expect(text)
        .toContain(
          'Read',
        );

      expect(
        fixture.nativeElement
          .querySelectorAll(
            '.notification-card--unread',
          )
          .length,
      ).toBe(1);
    });

    it('renders the polished hero, truthful page summary and guidance', async () => {
      await createComponent();

      const element =
        fixture.nativeElement as HTMLElement;

      expect(element.textContent)
        .toContain(
          'Stay on top of your updates',
        );

      expect(
        element.querySelector(
          '[data-testid="notification-total-count"]',
        )?.textContent?.trim(),
      ).toBe('2');

      expect(
        element.querySelector(
          '[data-testid="notification-unread-count"]',
        )?.textContent?.trim(),
      ).toBe('1');

      expect(
        element.querySelector(
          '[data-testid="notification-read-count"]',
        )?.textContent?.trim(),
      ).toBe('1');

      expect(element.textContent)
        .toContain(
          'Notification tips',
        );

      expect(element.textContent)
        .toContain(
          'Application updates',
        );

      expect(element.textContent)
        .not.toContain(
          'Contact updates',
        );

      expect(element.textContent)
        .not.toContain(
          'New message',
        );
    });

    it('marks one notification read and reloads server state', async () => {
      await createComponent();

      const unread =
        createPage().items[0];

      fixture.componentInstance
        .markRead(
          unread,
        );

      expect(
        service.readIds,
      ).toEqual([
        unread.id,
      ]);

      expect(
        service.queries.length,
      ).toBe(2);
    });

    it('marks all own notifications read and reloads the current page', async () => {
      await createComponent();

      fixture.componentInstance
        .markAllRead();

      expect(
        service.markAllCalls,
      ).toBe(1);

      expect(
        service.queries.length,
      ).toBe(2);

      expect(
        service.queries.at(-1),
      ).toEqual({
        page: 1,
        pageSize: 20,
      });
    });

    it('renders an empty state without external notification actions', async () => {
      await createComponent({
        items: [],
        page: 1,
        pageSize: 20,
        totalCount: 0,
      });

      const text =
        fixture.nativeElement
          .textContent as string;

      expect(text)
        .toContain(
          'No notifications yet',
        );

      expect(text)
        .toContain(
          'View my applications',
        );

      expect(text)
        .not.toContain(
          'Email notification',
        );
    });

    it('navigates to the Job Seeker application tracking context', async () => {
      await createComponent();

      fixture.componentInstance
        .viewApplications();

      expect(navigate)
        .toHaveBeenCalledWith([
          '/seeker/applications',
        ]);
    });
  },
);

class FakeNotificationsService {
  readonly queries:
    NotificationQuery[] =
      [];

  readonly readIds:
    string[] =
      [];

  markAllCalls =
    0;

  response:
    Observable<NotificationPage> =
      of(createPage());

  getOwnNotifications(
    query:
      NotificationQuery,
  ): Observable<NotificationPage> {
    this.queries.push({
      ...query,
    });

    return this.response;
  }

  markRead(
    notificationId: string,
  ): Observable<void> {
    this.readIds.push(
      notificationId,
    );

    return of(
      void 0,
    );
  }

  markAllRead():
    Observable<void> {
    this.markAllCalls++;

    return of(
      void 0,
    );
  }
}

function createPage():
  NotificationPage {
  return {
    items: [
      {
        id:
          '11111111-1111-1111-1111-111111111111',
        type:
          NotificationType.ApplicationStatusChanged,
        title:
          'Application status changed',
        message:
          'Your application is now shortlisted.',
        jobApplicationId:
          '33333333-3333-3333-3333-333333333333',
        isRead:
          false,
        createdAtUtc:
          '2026-09-12T12:00:00Z',
        readAtUtc:
          null,
      },
      {
        id:
          '22222222-2222-2222-2222-222222222222',
        type:
          NotificationType.ApplicationStatusChanged,
        title:
          'Application status changed',
        message:
          'Your application is under review.',
        jobApplicationId:
          '44444444-4444-4444-4444-444444444444',
        isRead:
          true,
        createdAtUtc:
          '2026-09-12T11:00:00Z',
        readAtUtc:
          '2026-09-12T11:30:00Z',
      },
    ],
    page: 1,
    pageSize: 20,
    totalCount: 2,
  };
}
