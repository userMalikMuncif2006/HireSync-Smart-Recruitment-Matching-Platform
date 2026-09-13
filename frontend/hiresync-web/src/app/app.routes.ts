import { Routes } from '@angular/router';

import { roleGuard } from './core/auth/role.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login',
  },
  {
    path: 'login',
    loadComponent: () =>
      import(
        './features/auth/login/login-page'
      ).then((module) => module.LoginPage),
  },
  {
    path: 'register',
    loadComponent: () =>
      import(
        './features/auth/register/register-page'
      ).then((module) => module.RegisterPage),
  },
  {
    path: 'verify-employer-email',
    loadComponent: () =>
      import(
        './features/auth/employer-email-verification/employer-email-verification-page'
      ).then(
        (module) =>
          module.EmployerEmailVerificationPage,
      ),
  },
  {
    path: 'activate-admin',
    loadComponent: () =>
      import(
        './features/auth/administrator-activation/administrator-activation-page'
      ).then(
        (module) =>
          module.AdministratorActivationPage,
      ),
  },
  {
    path: 'seeker/contact-requests',
    canMatch: [roleGuard],
    data: {
      role: 'JobSeeker',
    },
    loadComponent: () =>
      import(
        './features/seeker/contact-requests/job-seeker-contact-requests-page'
      ).then(
        (module) =>
          module.JobSeekerContactRequestsPage,
      ),
  },
  {
    path: 'seeker/notifications',
    canMatch: [roleGuard],
    data: {
      role: 'JobSeeker',
    },
    loadComponent: () =>
      import(
        './features/seeker/notifications/job-seeker-notifications-page'
      ).then(
        (module) =>
          module.JobSeekerNotificationsPage,
      ),
  },
  {
    path: 'seeker/applications',
    canMatch: [roleGuard],
    data: {
      role: 'JobSeeker',
    },
    loadComponent: () =>
      import(
        './features/seeker/applications/job-seeker-applications-page'
      ).then(
        (module) =>
          module.JobSeekerApplicationsPage,
      ),
  },
  {
    path: 'seeker/profile',
    canMatch: [roleGuard],
    data: {
      role: 'JobSeeker',
    },
    loadComponent: () =>
      import(
        './features/seeker/profile/job-seeker-profile-page'
      ).then(
        (module) =>
          module.JobSeekerProfilePage,
      ),
  },
  {
    path: 'seeker/vacancies',
    canMatch: [roleGuard],
    data: {
      role: 'JobSeeker',
    },
    loadComponent: () =>
      import(
        './features/seeker/vacancies/vacancy-search-page'
      ).then(
        (module) => module.VacancySearchPage,
      ),
  },
  {
    path: 'seeker/vacancies/:vacancyId',
    canMatch: [roleGuard],
    data: {
      role: 'JobSeeker',
    },
    loadComponent: () =>
      import(
        './features/seeker/matching/job-match-page'
      ).then((module) => module.JobMatchPage),
  },
  {
    path: 'seeker',
    canMatch: [roleGuard],
    data: {
      role: 'JobSeeker',
      title: 'Job Seeker Workspace',
      message:
        'Job Seeker features are provided through the assigned Job Seeker modules.',
    },
    loadComponent: () =>
      import(
        './core/routing/role-home-page'
      ).then((module) => module.RoleHomePage),
  },
  {
    path: 'employer',
    canMatch: [roleGuard],
    data: {
      role: 'Employer',
    },
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'profile',
      },
      {
        path: 'vacancies',
        loadComponent: () =>
          import(
            './features/employer/vacancies/employer-vacancy-list-page'
          ).then(
            (module) => module.EmployerVacancyListPage,
          ),
      },
      {
        path: 'vacancies/new',
        loadComponent: () =>
          import(
            './features/employer/vacancies/employer-vacancy-form-page'
          ).then(
            (module) => module.EmployerVacancyFormPage,
          ),
      },
      {
        path: 'vacancies/:vacancyId/edit',
        loadComponent: () =>
          import(
            './features/employer/vacancies/employer-vacancy-form-page'
          ).then(
            (module) => module.EmployerVacancyFormPage,
          ),
      },
      {
        path: 'vacancies/:vacancyId/applicants',
        loadComponent: () =>
          import(
            './features/employer/vacancies/ranked-applicants-page'
          ).then(
            (module) => module.RankedApplicantsPage,
          ),
      },
      {
        path: 'profile',
        loadComponent: () =>
          import(
            './features/employer/profile/employer-profile-page'
          ).then(
            (module) => module.EmployerProfilePage,
          ),
      },
    ],
  },
  {
    path: 'admin',
    canMatch: [roleGuard],
    data: {
      role: 'Administrator',
    },
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import(
            './features/administration/admin-dashboard-page'
          ).then(
            (module) =>
              module.AdminDashboardPage,
          ),
      },
      {
        path: 'users',
        loadComponent: () =>
          import(
            './features/administration/admin-users-page'
          ).then(
            (module) =>
              module.AdminUsersPage,
          ),
      },
      {
        path: 'employer-verification',
        loadComponent: () =>
          import(
            './features/administration/admin-employer-verification-page'
          ).then(
            (module) =>
              module.AdminEmployerVerificationPage,
          ),
      },
    ],
  },
  {
    path: '**',
    redirectTo: 'login',
  },
];




