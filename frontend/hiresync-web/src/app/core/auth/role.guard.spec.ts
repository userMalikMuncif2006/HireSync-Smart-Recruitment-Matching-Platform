import { TestBed } from '@angular/core/testing';
import {
  provideRouter,
  Route,
  Router,
  UrlTree,
} from '@angular/router';

import { AuthSessionService } from './auth-session.service';
import { roleGuard } from './role.guard';

describe('roleGuard', () => {
  let session: AuthSessionService;
  let router: Router;

  beforeEach(() => {
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        AuthSessionService,
      ],
    });

    session =
      TestBed.inject(AuthSessionService);

    router =
      TestBed.inject(Router);
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  function runGuard(requiredRole: string) {
    return TestBed.runInInjectionContext(
      () =>
        roleGuard(
          {
            data: {
              role: requiredRole,
            },
          } as Route,
          [],
        ),
    );
  }

  it('allows an authenticated user with the required role', () => {
    session.setSession({
      accessToken: 'employer-token',
      expiresAtUtc: '2099-01-01T00:00:00Z',
      userId: '11111111-1111-1111-1111-111111111111',
      email: 'employer@example.com',
      role: 'Employer',
    });

    expect(runGuard('Employer')).toBe(true);
  });

  it('redirects an unauthenticated user to login', () => {
    const result =
      runGuard('Employer') as UrlTree;

    expect(router.serializeUrl(result))
      .toBe('/login');
  });

  it('redirects a different role to its own role area', () => {
    session.setSession({
      accessToken: 'jobseeker-token',
      expiresAtUtc: '2099-01-01T00:00:00Z',
      userId: '22222222-2222-2222-2222-222222222222',
      email: 'seeker@example.com',
      role: 'JobSeeker',
    });

    const result =
      runGuard('Employer') as UrlTree;

    expect(router.serializeUrl(result))
      .toBe('/seeker');
  });
});