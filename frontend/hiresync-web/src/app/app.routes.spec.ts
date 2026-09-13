import {
  routes,
} from './app.routes';

import {
  roleGuard,
} from './core/auth/role.guard';

describe(
  'application routes',
  () => {
    it('lazy loads public authentication routes', () => {
      for (
        const path of [
          '',
          'login',
          'register',
          'verify-employer-email',
          'activate-admin',
        ]
      ) {
        const route =
          routes.find(
            (item) =>
              item.path === path,
          );

        expect(
          typeof route
            ?.loadComponent,
        ).toBe('function');
      }
    });

    it('uses one protected Job Seeker shell with dashboard and feature children', () => {
      const route =
        routes.find(
          (item) =>
            item.path === 'seeker',
        );

      expect(route?.canMatch)
        .toContain(roleGuard);

      expect(
        route?.data?.['role'],
      ).toBe('JobSeeker');

      expect(
        typeof route
          ?.loadComponent,
      ).toBe('function');

      const paths =
        route?.children?.map(
          (item) =>
            item.path,
        ) ?? [];

      expect(paths)
        .toEqual(
          expect.arrayContaining([
            '',
            'dashboard',
            'contact-requests',
            'notifications',
            'applications',
            'profile',
            'vacancies',
            'vacancies/:vacancyId',
          ]),
        );

      expect(
        route?.children?.find(
          (item) =>
            item.path === '',
        )?.redirectTo,
      ).toBe('dashboard');
    });

    it('uses one protected Employer shell with dashboard and existing vacancy workflows', () => {
      const route =
        routes.find(
          (item) =>
            item.path === 'employer',
        );

      expect(route?.canMatch)
        .toContain(roleGuard);

      expect(
        route?.data?.['role'],
      ).toBe('Employer');

      expect(
        typeof route
          ?.loadComponent,
      ).toBe('function');

      const paths =
        route?.children?.map(
          (item) =>
            item.path,
        ) ?? [];

      expect(paths)
        .toEqual(
          expect.arrayContaining([
            '',
            'dashboard',
            'profile',
            'vacancies',
            'vacancies/new',
            'vacancies/:vacancyId/edit',
            'vacancies/:vacancyId/applicants',
          ]),
        );

      expect(
        route?.children?.find(
          (item) =>
            item.path === '',
        )?.redirectTo,
      ).toBe('dashboard');
    });

    it('uses one protected Administrator shell with dashboard and management children', () => {
      const route =
        routes.find(
          (item) =>
            item.path === 'admin',
        );

      expect(route?.canMatch)
        .toContain(roleGuard);

      expect(
        route?.data?.['role'],
      ).toBe(
        'Administrator',
      );

      expect(
        typeof route
          ?.loadComponent,
      ).toBe('function');

      const paths =
        route?.children?.map(
          (item) =>
            item.path,
        ) ?? [];

      expect(paths)
        .toEqual(
          expect.arrayContaining([
            '',
            'dashboard',
            'users',
            'employer-verification',
          ]),
        );

      expect(
        route?.children?.find(
          (item) =>
            item.path === '',
        )?.redirectTo,
      ).toBe('dashboard');
    });

    it('keeps every role feature page lazy loaded', () => {
      const roleRoutes =
        routes.filter(
          (item) =>
            item.path === 'seeker' ||
            item.path === 'employer' ||
            item.path === 'admin',
        );

      for (
        const roleRoute of
          roleRoutes
      ) {
        for (
          const child of
            roleRoute.children ??
            []
        ) {
          if (
            child.path === ''
          ) {
            continue;
          }

          expect(
            typeof child
              .loadComponent,
          ).toBe('function');
        }
      }
    });
  },
);