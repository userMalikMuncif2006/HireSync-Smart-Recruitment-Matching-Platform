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
  shortLabel: string;
}

@Component({
  selector: 'app-role-shell-page',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  templateUrl: './role-shell-page.html',
  styleUrl: './role-shell-page.css',
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
            shortLabel: 'D',
          },
          {
            label: 'Find Jobs',
            route: '/seeker/vacancies',
            shortLabel: 'J',
          },
          {
            label: 'My Profile',
            route: '/seeker/profile',
            shortLabel: 'P',
          },
          {
            label: 'Applications',
            route: '/seeker/applications',
            shortLabel: 'A',
          },
          {
            label: 'Notifications',
            route: '/seeker/notifications',
            shortLabel: 'N',
          },
          {
            label: 'Contact Requests',
            route: '/seeker/contact-requests',
            shortLabel: 'C',
          },
        ];

      case 'Employer':
        return [
          {
            label: 'Dashboard',
            route: '/employer/dashboard',
            shortLabel: 'D',
          },
          {
            label: 'Company Profile',
            route: '/employer/profile',
            shortLabel: 'P',
          },
          {
            label: 'Vacancies',
            route: '/employer/vacancies',
            shortLabel: 'V',
          },
        ];

      case 'Administrator':
        return [
          {
            label: 'Dashboard',
            route: '/admin/dashboard',
            shortLabel: 'D',
          },
          {
            label: 'Accounts',
            route: '/admin/users',
            shortLabel: 'A',
          },
          {
            label: 'Employer Verification',
            route: '/admin/employer-verification',
            shortLabel: 'V',
          },
        ];

      default:
        return [];
    }
  }
}