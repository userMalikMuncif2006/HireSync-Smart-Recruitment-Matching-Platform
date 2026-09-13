import {
  Component,
  inject,
} from '@angular/core';
import {
  ActivatedRoute,
  RouterLink,
} from '@angular/router';

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

  company:
    'M4 21V7l8-4 8 4v14M8 21v-5h8v5M8 9h.01M12 9h.01M16 9h.01',

  vacancies:
    'M9 6V4h6v2m-10 0h14v14H5V6Zm4 5h3M8 11h1',
} as const;

@Component({
  selector: 'app-workspace-dashboard-page',
  standalone: true,
  imports: [
    RouterLink,
  ],
  templateUrl:
    './workspace-dashboard-page.html',
  styleUrl:
    './workspace-dashboard-page.css',
})
export class WorkspaceDashboardPage {
  private readonly route =
    inject(ActivatedRoute);

  readonly configuration =
    this.readConfiguration(
      this.route.snapshot.data[
        'workspaceRole'
      ] as WorkspaceRole,
    );

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
        'Move quickly between job discovery, your profile, applications and recruitment updates.',
      actions: [
        {
          title:
            'Find jobs',
          description:
            'Search vacancies and review your authoritative match results.',
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
            'Review unread and previous application-status notifications.',
          route:
            '/seeker/notifications',
          iconPath:
            icons.notifications,
        },
      ],
    };
  }
}