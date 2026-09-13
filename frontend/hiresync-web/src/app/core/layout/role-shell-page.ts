import {
  computed,
  Component,
  inject,
  signal,
} from '@angular/core';
import {
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';

import {
  AuthRole,
} from '../auth/auth.models';
import {
  AuthSessionService,
} from '../auth/auth-session.service';

interface WorkspaceNavigationItem {
  label: string;
  route: string;
  iconPath: string;
}

const icons = {
  dashboard:
    'M4 4h6v6H4V4Zm10 0h6v10h-6V4ZM4 14h6v6H4v-6Zm10 4h6v2h-6v-2Z',

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

  accounts:
    'M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm-6 10a6 6 0 0 1 12 0m2-10a3 3 0 1 0 0-6m1 10a5 5 0 0 1 3 4.5',

  verification:
    'M12 3l8 4v5c0 5-3.4 8.7-8 10-4.6-1.3-8-5-8-10V7l8-4Zm-3 9 2 2 4-4',
} as const;

@Component({
  selector: 'app-role-shell-page',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  templateUrl: './role-shell-page.html',
  styleUrls: [
    './role-shell-page.css',
    './role-shell-responsive.css',
  ],
})
export class RoleShellPage {
  private readonly session =
    inject(AuthSessionService);

  private readonly router =
    inject(Router);

  readonly menuOpen =
    signal(false);

  readonly role =
    computed(
      () =>
        this.session.role(),
    );

  readonly email =
    computed(
      () =>
        this.session.session()?.email ??
        '',
    );

  readonly roleLabel =
    computed(
      () =>
        this.readRoleLabel(
          this.role(),
        ),
    );

  readonly avatarLabel =
    computed(() => {
      switch (this.role()) {
        case 'JobSeeker':
          return 'J';

        case 'Employer':
          return 'E';

        case 'Administrator':
          return 'A';

        default:
          return 'H';
      }
    });

  readonly navigation =
    computed(
      () =>
        this.readNavigation(
          this.role(),
        ),
    );

  toggleMenu(): void {
    this.menuOpen.update(
      (current) => !current,
    );
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }

  logout(): void {
    this.session.clearSession();

    void this.router.navigate([
      '/login',
    ]);
  }

  private readRoleLabel(
    role: AuthRole | null,
  ): string {
    switch (role) {
      case 'JobSeeker':
        return 'Job Seeker';

      case 'Employer':
        return 'Employer';

      case 'Administrator':
        return 'Administrator';

      default:
        return 'HireSync';
    }
  }

  private readNavigation(
    role: AuthRole | null,
  ): WorkspaceNavigationItem[] {
    switch (role) {
      case 'JobSeeker':
        return [
          {
            label: 'Dashboard',
            route: '/seeker/dashboard',
            iconPath: icons.dashboard,
          },
          {
            label: 'Find Jobs',
            route: '/seeker/vacancies',
            iconPath: icons.search,
          },
          {
            label: 'My Profile',
            route: '/seeker/profile',
            iconPath: icons.profile,
          },
          {
            label: 'Applications',
            route: '/seeker/applications',
            iconPath: icons.applications,
          },
          {
            label: 'Notifications',
            route: '/seeker/notifications',
            iconPath: icons.notifications,
          },
          {
            label: 'Contact Requests',
            route: '/seeker/contact-requests',
            iconPath: icons.contact,
          },
        ];

      case 'Employer':
        return [
          {
            label: 'Dashboard',
            route: '/employer/dashboard',
            iconPath: icons.dashboard,
          },
          {
            label: 'Company Profile',
            route: '/employer/profile',
            iconPath: icons.company,
          },
          {
            label: 'Vacancies',
            route: '/employer/vacancies',
            iconPath: icons.vacancies,
          },
        ];

      case 'Administrator':
        return [
          {
            label: 'Dashboard',
            route: '/admin/dashboard',
            iconPath: icons.dashboard,
          },
          {
            label: 'Accounts',
            route: '/admin/users',
            iconPath: icons.accounts,
          },
          {
            label: 'Employer Verification',
            route: '/admin/employer-verification',
            iconPath: icons.verification,
          },
        ];

      default:
        return [];
    }
  }
}