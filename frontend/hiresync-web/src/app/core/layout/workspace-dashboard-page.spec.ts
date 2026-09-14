import {
  HttpErrorResponse,
} from '@angular/common/http';
import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  ActivatedRoute,
} from '@angular/router';
import {
  NEVER,
  Observable,
  of,
  throwError,
} from 'rxjs';

import {
  ApplicationStatus,
  JobSeekerApplicationPage,
  VacancyStatus,
} from '../../features/seeker/applications/job-seeker-applications.models';
import {
  JobSeekerApplicationsService,
} from '../../features/seeker/applications/job-seeker-applications.service';
import {
  ContactRequestStatus,
  JobSeekerContactRequest,
} from '../../features/seeker/contact-requests/job-seeker-contact-requests.models';
import {
  JobSeekerContactRequestsService,
} from '../../features/seeker/contact-requests/job-seeker-contact-requests.service';
import {
  NotificationPage,
  NotificationType,
} from '../../features/seeker/notifications/job-seeker-notifications.models';
import {
  JobSeekerNotificationsService,
} from '../../features/seeker/notifications/job-seeker-notifications.service';
import {
  EducationLevel,
  JobSeekerProfile,
} from '../../features/seeker/profile/job-seeker-profile.models';
import {
  JobSeekerProfileService,
} from '../../features/seeker/profile/job-seeker-profile.service';
import {
  WorkspaceDashboardPage,
} from './workspace-dashboard-page';

describe(
  'WorkspaceDashboardPage',
  () => {
    async function setup(
      role:
        | 'JobSeeker'
        | 'Employer',
      configure?: (
        services: TestServices,
      ) => void,
    ): Promise<{
      fixture:
        ComponentFixture<
          WorkspaceDashboardPage
        >;
      services:
        TestServices;
    }> {
      const services:
        TestServices = {
          profile:
            new FakeProfileService(),
          applications:
            new FakeApplicationsService(),
          notifications:
            new FakeNotificationsService(),
          contacts:
            new FakeContactRequestsService(),
        };

      configure?.(
        services,
      );

      await TestBed
        .configureTestingModule({
          imports: [
            WorkspaceDashboardPage,
          ],
          providers: [
            {
              provide:
                ActivatedRoute,
              useValue: {
                snapshot: {
                  data: {
                    workspaceRole:
                      role,
                  },
                },
              },
            },
            {
              provide:
                JobSeekerProfileService,
              useValue:
                services.profile,
            },
            {
              provide:
                JobSeekerApplicationsService,
              useValue:
                services.applications,
            },
            {
              provide:
                JobSeekerNotificationsService,
              useValue:
                services.notifications,
            },
            {
              provide:
                JobSeekerContactRequestsService,
              useValue:
                services.contacts,
            },
          ],
        })
        .compileComponents();

      const fixture =
        TestBed.createComponent(
          WorkspaceDashboardPage,
        );

      fixture.detectChanges();

      return {
        fixture,
        services,
      };
    }

    it('renders live Job Seeker overview without inventing a readiness percentage', async () => {
      const {
        fixture,
      } =
        await setup(
          'JobSeeker',
        );

      const element =
        fixture.nativeElement as HTMLElement;

      expect(
        element.textContent,
      ).toContain(
        'Career dashboard',
      );

      expect(
        textOf(
          element,
          '[data-testid="profile-readiness-value"]',
        ),
      ).toBe(
        'Ready',
      );

      expect(
        textOf(
          element,
          '[data-testid="applications-total-value"]',
        ),
      ).toBe(
        '4',
      );

      expect(
        textOf(
          element,
          '[data-testid="notifications-total-value"]',
        ),
      ).toBe(
        '3',
      );

      expect(
        textOf(
          element,
          '[data-testid="contact-pending-value"]',
        ),
      ).toBe(
        '1',
      );

      expect(
        element.textContent,
      ).not.toContain(
        '85%',
      );

      expect(
        element.textContent,
      ).toContain(
        'Frontend Developer',
      );

      expect(
        element.textContent,
      ).toContain(
        'Application shortlisted',
      );

      expect(
        element.querySelectorAll(
          '[data-testid="recent-application-row"]',
        ).length,
      ).toBe(
        2,
      );

      expect(
        element.querySelectorAll(
          '[data-testid="latest-notification-row"]',
        ).length,
      ).toBe(
        2,
      );
    });

    it('uses backend isMatchReady and renders clean empty states', async () => {
      const {
        fixture,
      } =
        await setup(
          'JobSeeker',
          (
            services,
          ) => {
            services.profile.result =
              of({
                ...readyProfile,
                isMatchReady:
                  false,
              });

            services.applications.result =
              of(
                emptyApplications,
              );

            services.notifications.result =
              of(
                emptyNotifications,
              );

            services.contacts.result =
              of([]);
          },
        );

      const element =
        fixture.nativeElement as HTMLElement;

      expect(
        textOf(
          element,
          '[data-testid="profile-readiness-value"]',
        ),
      ).toBe(
        'Needs details',
      );

      expect(
        element.textContent,
      ).toContain(
        'No applications yet.',
      );

      expect(
        element.textContent,
      ).toContain(
        'No notifications yet.',
      );

      expect(
        textOf(
          element,
          '[data-testid="contact-pending-value"]',
        ),
      ).toBe(
        '0',
      );
    });

    it('treats a missing profile as needs details', async () => {
      const {
        fixture,
      } =
        await setup(
          'JobSeeker',
          (
            services,
          ) => {
            services.profile.result =
              throwError(
                () =>
                  new HttpErrorResponse({
                    status: 404,
                  }),
              );
          },
        );

      const element =
        fixture.nativeElement as HTMLElement;

      expect(
        textOf(
          element,
          '[data-testid="profile-readiness-value"]',
        ),
      ).toBe(
        'Needs details',
      );
    });
    it('keeps successful dashboard data visible when one request fails', async () => {
      const {
        fixture,
      } =
        await setup(
          'JobSeeker',
          (
            services,
          ) => {
            services.profile.result =
              throwError(
                () =>
                  new Error(
                    'profile unavailable',
                  ),
              );
          },
        );

      const element =
        fixture.nativeElement as HTMLElement;

      expect(
        textOf(
          element,
          '[data-testid="profile-readiness-value"]',
        ),
      ).toBe(
        'Unavailable',
      );

      expect(
        textOf(
          element,
          '[data-testid="applications-total-value"]',
        ),
      ).toBe(
        '4',
      );

      expect(
        element.textContent,
      ).toContain(
        'Frontend Developer',
      );
    });

    it('renders an independent loading state without blocking other cards', async () => {
      const {
        fixture,
      } =
        await setup(
          'JobSeeker',
          (
            services,
          ) => {
            services.profile.result =
              NEVER;
          },
        );

      const element =
        fixture.nativeElement as HTMLElement;

      expect(
        textOf(
          element,
          '[data-testid="profile-readiness-value"]',
        ),
      ).toBe(
        'Loading',
      );

      expect(
        textOf(
          element,
          '[data-testid="applications-total-value"]',
        ),
      ).toBe(
        '4',
      );
    });

    it('preserves the existing Employer dashboard and does not load Job Seeker data', async () => {
      const {
        fixture,
        services,
      } =
        await setup(
          'Employer',
        );

      const element =
        fixture.nativeElement as HTMLElement;

      expect(
        element.textContent,
      ).toContain(
        'Recruitment dashboard',
      );

      expect(
        element.textContent,
      ).toContain(
        'Your recruitment workflow',
      );

      expect(
        element.textContent,
      ).toContain(
        'Manage vacancies',
      );

      expect(
        services.profile.calls,
      ).toBe(
        0,
      );

      expect(
        services.applications.calls,
      ).toBe(
        0,
      );

      expect(
        services.notifications.calls,
      ).toBe(
        0,
      );

      expect(
        services.contacts.calls,
      ).toBe(
        0,
      );
    });
  },
);

interface TestServices {
  profile:
    FakeProfileService;
  applications:
    FakeApplicationsService;
  notifications:
    FakeNotificationsService;
  contacts:
    FakeContactRequestsService;
}

class FakeProfileService {
  calls = 0;

  result:
    Observable<JobSeekerProfile> =
      of(
        readyProfile,
      );

  getOwnProfile():
    Observable<JobSeekerProfile> {
    this.calls++;

    return this.result;
  }
}

class FakeApplicationsService {
  calls = 0;

  result:
    Observable<JobSeekerApplicationPage> =
      of(
        applicationPage,
      );

  getOwnApplications():
    Observable<JobSeekerApplicationPage> {
    this.calls++;

    return this.result;
  }
}

class FakeNotificationsService {
  calls = 0;

  result:
    Observable<NotificationPage> =
      of(
        notificationPage,
      );

  getOwnNotifications():
    Observable<NotificationPage> {
    this.calls++;

    return this.result;
  }
}

class FakeContactRequestsService {
  calls = 0;

  result:
    Observable<JobSeekerContactRequest[]> =
      of(
        contactRequests,
      );

  getOwnContactRequests():
    Observable<JobSeekerContactRequest[]> {
    this.calls++;

    return this.result;
  }
}

const readyProfile:
  JobSeekerProfile = {
    totalExperienceMonths:
      24,
    educationLevel:
      EducationLevel.Bachelor,
    preferredLocation:
      'Colombo',
    skills: [],
    isMatchReady:
      true,
  };

const applicationPage:
  JobSeekerApplicationPage = {
    page: 1,
    pageSize: 2,
    totalCount: 4,
    items: [
      {
        applicationId:
          '11111111-1111-1111-1111-111111111111',
        vacancyId:
          '21111111-1111-1111-1111-111111111111',
        vacancyTitle:
          'Frontend Developer',
        companyName:
          'ABC Company',
        vacancyLocation:
          'Colombo',
        vacancyStatus:
          VacancyStatus.Open,
        status:
          ApplicationStatus.UnderReview,
        appliedAtUtc:
          '2026-09-12T08:00:00Z',
        updatedAtUtc:
          '2026-09-13T08:00:00Z',
      },
      {
        applicationId:
          '31111111-1111-1111-1111-111111111111',
        vacancyId:
          '41111111-1111-1111-1111-111111111111',
        vacancyTitle:
          '.NET Developer',
        companyName:
          'XYZ Company',
        vacancyLocation:
          'Kandy',
        vacancyStatus:
          VacancyStatus.Open,
        status:
          ApplicationStatus.Shortlisted,
        appliedAtUtc:
          '2026-09-10T08:00:00Z',
        updatedAtUtc:
          '2026-09-11T08:00:00Z',
      },
    ],
  };

const emptyApplications:
  JobSeekerApplicationPage = {
    page: 1,
    pageSize: 2,
    totalCount: 0,
    items: [],
  };

const notificationPage:
  NotificationPage = {
    page: 1,
    pageSize: 2,
    totalCount: 3,
    items: [
      {
        id:
          '51111111-1111-1111-1111-111111111111',
        type:
          NotificationType.ApplicationStatusChanged,
        title:
          'Application shortlisted',
        message:
          'Your application status changed to Shortlisted.',
        jobApplicationId:
          '11111111-1111-1111-1111-111111111111',
        isRead:
          false,
        createdAtUtc:
          '2026-09-13T09:00:00Z',
        readAtUtc:
          null,
      },
      {
        id:
          '61111111-1111-1111-1111-111111111111',
        type:
          NotificationType.ApplicationStatusChanged,
        title:
          'Application moved to review',
        message:
          'Your application status changed to Under Review.',
        jobApplicationId:
          '31111111-1111-1111-1111-111111111111',
        isRead:
          true,
        createdAtUtc:
          '2026-09-12T09:00:00Z',
        readAtUtc:
          '2026-09-12T10:00:00Z',
      },
    ],
  };

const emptyNotifications:
  NotificationPage = {
    page: 1,
    pageSize: 2,
    totalCount: 0,
    items: [],
  };

const contactRequests:
  JobSeekerContactRequest[] = [
    {
      id:
        '71111111-1111-1111-1111-111111111111',
      jobApplicationId:
        '11111111-1111-1111-1111-111111111111',
      vacancyId:
        '21111111-1111-1111-1111-111111111111',
      vacancyTitle:
        'Frontend Developer',
      employerCompanyName:
        'ABC Company',
      status:
        ContactRequestStatus.Pending,
      requestedAtUtc:
        '2026-09-13T11:00:00Z',
      respondedAtUtc:
        null,
      rowVersion:
        'row-version-1',
    },
    {
      id:
        '81111111-1111-1111-1111-111111111111',
      jobApplicationId:
        '31111111-1111-1111-1111-111111111111',
      vacancyId:
        '41111111-1111-1111-1111-111111111111',
      vacancyTitle:
        '.NET Developer',
      employerCompanyName:
        'XYZ Company',
      status:
        ContactRequestStatus.Accepted,
      requestedAtUtc:
        '2026-09-10T11:00:00Z',
      respondedAtUtc:
        '2026-09-11T11:00:00Z',
      rowVersion:
        'row-version-2',
    },
  ];

function textOf(
  root: HTMLElement,
  selector: string,
): string {
  const element =
    root.querySelector(
      selector,
    );

  expect(
    element,
  ).not.toBeNull();

  return (
    element?.textContent
      ?.trim() ?? ''
  );
}
