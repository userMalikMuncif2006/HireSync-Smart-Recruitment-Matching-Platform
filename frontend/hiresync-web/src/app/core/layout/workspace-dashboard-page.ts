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
  shortLabel: string;
}

interface DashboardConfiguration {
  eyebrow: string;
  title: string;
  description: string;
  actions: DashboardAction[];
}

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
            shortLabel:
              'V',
          },
          {
            title:
              'Company profile',
            description:
              'Review and maintain your Employer profile information.',
            route:
              '/employer/profile',
            shortLabel:
              'P',
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
            'Search available vacancies and review your server-calculated match.',
          route:
            '/seeker/vacancies',
          shortLabel:
            'J',
        },
        {
          title:
            'My profile',
          description:
            'Maintain your structured profile, skills and match-readiness information.',
          route:
            '/seeker/profile',
          shortLabel:
            'P',
        },
        {
          title:
            'Applications',
          description:
            'Track the latest status of every application you submitted.',
          route:
            '/seeker/applications',
          shortLabel:
            'A',
        },
        {
          title:
            'Notifications',
          description:
            'Review unread and previous application-status notifications.',
          route:
            '/seeker/notifications',
          shortLabel:
            'N',
        },
      ],
    };
  }
}