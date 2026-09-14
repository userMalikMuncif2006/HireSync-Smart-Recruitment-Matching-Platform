import {
  DatePipe,
} from '@angular/common';
import {
  HttpErrorResponse,
} from '@angular/common/http';
import {
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import {
  takeUntilDestroyed,
} from '@angular/core/rxjs-interop';
import {
  ActivatedRoute,
  RouterLink,
} from '@angular/router';
import {
  forkJoin,
} from 'rxjs';

import {
  EmployerProfile,
} from '../../features/employer/profile/employer-profile.models';
import {
  EmployerProfileService,
} from '../../features/employer/profile/employer-profile.service';
import {
  EmployerVacancyListItem,
  EmployerVacancyPage,
  VacancyStatus as EmployerVacancyStatus,
} from '../../features/employer/vacancies/employer-vacancy.models';
import {
  EmployerVacancyService,
} from '../../features/employer/vacancies/employer-vacancy.service';
import {
  JobSeekerApplicationsService,
} from '../../features/seeker/applications/job-seeker-applications.service';
import {
  ApplicationStatus,
  JobSeekerApplicationListItem,
  JobSeekerApplicationPage,
} from '../../features/seeker/applications/job-seeker-applications.models';
import {
  ContactRequestStatus,
  JobSeekerContactRequest,
} from '../../features/seeker/contact-requests/job-seeker-contact-requests.models';
import {
  JobSeekerContactRequestsService,
} from '../../features/seeker/contact-requests/job-seeker-contact-requests.service';
import {
  JobSeekerNotification,
  NotificationPage,
} from '../../features/seeker/notifications/job-seeker-notifications.models';
import {
  JobSeekerNotificationsService,
} from '../../features/seeker/notifications/job-seeker-notifications.service';
import {
  JobSeekerProfile,
} from '../../features/seeker/profile/job-seeker-profile.models';
import {
  JobSeekerProfileService,
} from '../../features/seeker/profile/job-seeker-profile.service';

type WorkspaceRole =
  | 'JobSeeker'
  | 'Employer';

interface DashboardAction {
  title: string;
  description: string;
  route: string;
  iconPath: string;
}

interface DashboardConfiguration {
  eyebrow: string;
  title: string;
  description: string;
  actions: DashboardAction[];
}

const icons = {
  search:
    'M11 19a8 8 0 1 1 0-16 8 8 0 0 1 0 16Zm10 2-4.35-4.35',

  profile:
    'M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm-7 8a7 7 0 0 1 14 0',

  applications:
    'M6 3h12v18H6V3Zm3 4h6M9 11h6M9 15h4',

  notifications:
    'M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4',

  contact:
    'M4 5h16v12H7l-3 3V5Zm4 5h8',

  company:
    'M4 21V7l8-4 8 4v14M8 21v-5h8v5M8 9h.01M12 9h.01M16 9h.01',

  vacancies:
    'M9 6V4h6v2m-10 0h14v14H5V6Zm4 5h3M8 11h1',
} as const;

@Component({
  selector: 'app-workspace-dashboard-page',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
  ],
  templateUrl:
    './workspace-dashboard-page.html',
  styleUrls: [
    './workspace-dashboard-page.css',
    './workspace-dashboard-jobseeker.css',
    './workspace-dashboard-jobseeker-activity.css',
    './workspace-dashboard-employer.css',
  ],
})
export class WorkspaceDashboardPage
  implements OnInit {
  private readonly route =
    inject(ActivatedRoute);

  private readonly destroyRef =
    inject(DestroyRef);

  private readonly employerProfileService =
    inject(EmployerProfileService);

  private readonly employerVacancyService =
    inject(EmployerVacancyService);

  private readonly profileService =
    inject(JobSeekerProfileService);

  private readonly applicationsService =
    inject(JobSeekerApplicationsService);

  private readonly notificationsService =
    inject(JobSeekerNotificationsService);

  private readonly contactRequestsService =
    inject(JobSeekerContactRequestsService);

  readonly workspaceRole =
    this.route.snapshot.data[
      'workspaceRole'
    ] as WorkspaceRole;

  readonly isJobSeeker =
    this.workspaceRole ===
    'JobSeeker';

  readonly isEmployer =
    this.workspaceRole ===
    'Employer';

  readonly configuration =
    this.readConfiguration(
      this.workspaceRole,
    );

  readonly profile =
    signal<JobSeekerProfile | null>(
      null,
    );

  readonly applications =
    signal<JobSeekerApplicationPage | null>(
      null,
    );

  readonly notifications =
    signal<NotificationPage | null>(
      null,
    );

  readonly contactRequests =
    signal<
      JobSeekerContactRequest[] | null
    >(
      null,
    );

  readonly employerProfile =
    signal<EmployerProfile | null>(
      null,
    );

  readonly employerVacancies =
    signal<EmployerVacancyPage | null>(
      null,
    );

  readonly employerOpenVacancyCount =
    signal<number | null>(
      null,
    );

  readonly employerClosedVacancyCount =
    signal<number | null>(
      null,
    );

  readonly employerProfileLoading =
    signal(false);

  readonly employerVacanciesLoading =
    signal(false);

  readonly employerProfileError =
    signal(false);

  readonly employerVacanciesError =
    signal(false);

  readonly profileLoading =
    signal(false);

  readonly applicationsLoading =
    signal(false);

  readonly notificationsLoading =
    signal(false);

  readonly contactRequestsLoading =
    signal(false);

  readonly profileError =
    signal(false);

  readonly applicationsError =
    signal(false);

  readonly notificationsError =
    signal(false);

  readonly contactRequestsError =
    signal(false);

  readonly employerProfileStatusLabel =
    computed(() => {
      const profile =
        this.employerProfile();

      return profile?.isProfileComplete
        ? 'Complete'
        : 'Needs details';
    });

  readonly employerWelcomeLabel =
    computed(() => {
      const companyName =
        this.employerProfile()
          ?.companyName
          ?.trim();

      return companyName
        ? `Welcome back, ${companyName}.`
        : 'Welcome back, Employer.';
    });

  readonly recentEmployerVacancies =
    computed<EmployerVacancyListItem[]>(
      () =>
        this.employerVacancies()
          ?.items
          .slice(
            0,
            3,
          ) ?? [],
    );

  readonly profileReadinessLabel =
    computed(() => {
      const profile =
        this.profile();

      if (!profile) {
        return 'Needs details';
      }

      return profile.isMatchReady
        ? 'Ready'
        : 'Needs details';
    });

  readonly profileNextStepText =
    computed(() => {
      const profile =
        this.profile();

      if (profile?.isMatchReady) {
        return 'Review your structured profile and keep your skills, experience and preferences current.';
      }

      return 'Complete your structured profile details so HireSync can determine match readiness.';
    });

  readonly pendingContactRequestCount =
    computed(() => {
      const requests =
        this.contactRequests();

      if (requests === null) {
        return null;
      }

      return requests.filter(
        (request) =>
          request.status ===
          ContactRequestStatus.Pending,
      ).length;
    });

  readonly recentApplications =
    computed<
      JobSeekerApplicationListItem[]
    >(
      () =>
        this.applications()
          ?.items
          .slice(
            0,
            2,
          ) ?? [],
    );

  readonly latestNotifications =
    computed<
      JobSeekerNotification[]
    >(
      () =>
        this.notifications()
          ?.items
          .slice(
            0,
            2,
          ) ?? [],
    );

  ngOnInit(): void {
    if (this.isJobSeeker) {
      this.loadProfile();
      this.loadApplications();
      this.loadNotifications();
      this.loadContactRequests();

      return;
    }

    this.loadEmployerProfile();
    this.loadEmployerVacancyOverview();
  }

  employerVacancyStatusLabel(
    status: EmployerVacancyStatus,
  ): string {
    return status === 1
      ? 'Open'
      : 'Closed';
  }

  applicationStatusLabel(
    status: ApplicationStatus,
  ): string {
    switch (status) {
      case ApplicationStatus.Applied:
        return 'Applied';

      case ApplicationStatus.UnderReview:
        return 'Under review';

      case ApplicationStatus.Shortlisted:
        return 'Shortlisted';

      case ApplicationStatus.Selected:
        return 'Selected';

      case ApplicationStatus.Rejected:
        return 'Rejected';

      default:
        return 'Updated';
    }
  }

  private loadEmployerProfile(): void {
    this.employerProfileLoading.set(
      true,
    );

    this.employerProfileError.set(
      false,
    );

    this.employerProfileService
      .getOwnProfile()
      .pipe(
        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: (profile) => {
          this.employerProfile.set(
            profile,
          );

          this.employerProfileLoading.set(
            false,
          );
        },

        error: (
          error:
            HttpErrorResponse,
        ) => {
          if (error.status === 404) {
            this.employerProfile.set(
              null,
            );

            this.employerProfileError.set(
              false,
            );

            this.employerProfileLoading.set(
              false,
            );

            return;
          }

          this.employerProfileError.set(
            true,
          );

          this.employerProfileLoading.set(
            false,
          );
        },
      });
  }

  private loadEmployerVacancyOverview(): void {
    this.employerVacanciesLoading.set(
      true,
    );

    this.employerVacanciesError.set(
      false,
    );

    forkJoin({
      all:
        this.employerVacancyService
          .getVacancies(
            null,
            1,
            3,
          ),
      open:
        this.employerVacancyService
          .getVacancies(
            1,
            1,
            1,
          ),
      closed:
        this.employerVacancyService
          .getVacancies(
            2,
            1,
            1,
          ),
    })
      .pipe(
        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: ({
          all,
          open,
          closed,
        }) => {
          this.employerVacancies.set(
            all,
          );

          this.employerOpenVacancyCount.set(
            open.totalCount,
          );

          this.employerClosedVacancyCount.set(
            closed.totalCount,
          );

          this.employerVacanciesLoading.set(
            false,
          );
        },

        error: () => {
          this.employerVacanciesError.set(
            true,
          );

          this.employerVacanciesLoading.set(
            false,
          );
        },
      });
  }

  private loadProfile(): void {
    this.profileLoading.set(
      true,
    );

    this.profileError.set(
      false,
    );

    this.profileService
      .getOwnProfile()
      .pipe(
        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: (profile) => {
          this.profile.set(
            profile,
          );

          this.profileLoading.set(
            false,
          );
        },

        error: (
          error:
            HttpErrorResponse,
        ) => {
          if (error.status === 404) {
            this.profile.set(
              null,
            );

            this.profileError.set(
              false,
            );

            this.profileLoading.set(
              false,
            );

            return;
          }

          this.profileError.set(
            true,
          );

          this.profileLoading.set(
            false,
          );
        },
      });
  }

  private loadApplications(): void {
    this.applicationsLoading.set(
      true,
    );

    this.applicationsError.set(
      false,
    );

    this.applicationsService
      .getOwnApplications({
        page: 1,
        pageSize: 2,
      })
      .pipe(
        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: (applications) => {
          this.applications.set(
            applications,
          );

          this.applicationsLoading.set(
            false,
          );
        },

        error: () => {
          this.applicationsError.set(
            true,
          );

          this.applicationsLoading.set(
            false,
          );
        },
      });
  }

  private loadNotifications(): void {
    this.notificationsLoading.set(
      true,
    );

    this.notificationsError.set(
      false,
    );

    this.notificationsService
      .getOwnNotifications({
        page: 1,
        pageSize: 2,
      })
      .pipe(
        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: (notifications) => {
          this.notifications.set(
            notifications,
          );

          this.notificationsLoading.set(
            false,
          );
        },

        error: () => {
          this.notificationsError.set(
            true,
          );

          this.notificationsLoading.set(
            false,
          );
        },
      });
  }

  private loadContactRequests(): void {
    this.contactRequestsLoading.set(
      true,
    );

    this.contactRequestsError.set(
      false,
    );

    this.contactRequestsService
      .getOwnContactRequests()
      .pipe(
        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: (requests) => {
          this.contactRequests.set(
            requests,
          );

          this.contactRequestsLoading.set(
            false,
          );
        },

        error: () => {
          this.contactRequestsError.set(
            true,
          );

          this.contactRequestsLoading.set(
            false,
          );
        },
      });
  }

  private readConfiguration(
    role: WorkspaceRole,
  ): DashboardConfiguration {
    if (role === 'Employer') {
      return {
        eyebrow:
          'Employer workspace',
        title:
          'Recruitment dashboard',
        description:
          'Manage your company profile, vacancies and applicant review workflow from one workspace.',
        actions: [
          {
            title:
              'Manage vacancies',
            description:
              'Create, edit, review and close your organisation vacancies.',
            route:
              '/employer/vacancies',
            iconPath:
              icons.vacancies,
          },
          {
            title:
              'Company profile',
            description:
              'Review and maintain your Employer profile information.',
            route:
              '/employer/profile',
            iconPath:
              icons.company,
          },
        ],
      };
    }

    return {
      eyebrow:
        'Job Seeker workspace',
      title:
        'Career dashboard',
      description:
        'Track your job search, applications and recruitment updates.',
      actions: [
        {
          title:
            'Find jobs',
          description:
            'Discover vacancies that match your structured profile.',
          route:
            '/seeker/vacancies',
          iconPath:
            icons.search,
        },
        {
          title:
            'My profile',
          description:
            'Maintain your structured profile, skills and match-readiness information.',
          route:
            '/seeker/profile',
          iconPath:
            icons.profile,
        },
        {
          title:
            'Applications',
          description:
            'Track the latest state of every application you submitted.',
          route:
            '/seeker/applications',
          iconPath:
            icons.applications,
        },
        {
          title:
            'Notifications',
          description:
            'Review application-status notifications.',
          route:
            '/seeker/notifications',
          iconPath:
            icons.notifications,
        },
      ],
    };
  }
}
