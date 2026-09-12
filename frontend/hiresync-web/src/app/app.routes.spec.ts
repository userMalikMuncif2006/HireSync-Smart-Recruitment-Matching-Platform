import { routes } from './app.routes';
import { roleGuard } from './core/auth/role.guard';

describe('application routes', () => {
  it('lazy loads the login page', () => {
    const route =
      routes.find((item) => item.path === 'login');

    expect(typeof route?.loadComponent)
      .toBe('function');
  });

  it('protects the Job Seeker role area', () => {
    const route =
      routes.find((item) => item.path === 'seeker');

    expect(route?.canMatch).toContain(roleGuard);
    expect(route?.data?.['role'])
      .toBe('JobSeeker');
    expect(typeof route?.loadComponent)
      .toBe('function');
  });

  it('protects and lazy loads the Job Seeker vacancy search page', () => {
    const route =
      routes.find(
        (item) =>
          item.path === 'seeker/vacancies',
      );

    expect(route?.canMatch)
      .toContain(roleGuard);

    expect(route?.data?.['role'])
      .toBe('JobSeeker');

    expect(typeof route?.loadComponent)
      .toBe('function');
  });

  it('protects and lazy loads the Job Seeker vacancy detail page', () => {
    const route =
      routes.find(
        (item) =>
          item.path ===
          'seeker/vacancies/:vacancyId',
      );

    expect(route?.canMatch).toContain(roleGuard);
    expect(route?.data?.['role'])
      .toBe('JobSeeker');
    expect(typeof route?.loadComponent)
      .toBe('function');
  });

  it('protects the Employer area and exposes the profile page', () => {
    const route =
      routes.find((item) => item.path === 'employer');

    expect(route?.canMatch).toContain(roleGuard);
    expect(route?.data?.['role']).toBe('Employer');

    const defaultRoute =
      route?.children?.find(
        (item) => item.path === '',
      );

    const profileRoute =
      route?.children?.find(
        (item) => item.path === 'profile',
      );

    expect(defaultRoute?.redirectTo)
      .toBe('profile');

    expect(typeof profileRoute?.loadComponent)
      .toBe('function');
  });

  it('protects the Administrator role area', () => {
    const route =
      routes.find((item) => item.path === 'admin');

    expect(route?.canMatch).toContain(roleGuard);
    expect(route?.data?.['role'])
      .toBe('Administrator');
    expect(typeof route?.loadComponent)
      .toBe('function');
  });
});
