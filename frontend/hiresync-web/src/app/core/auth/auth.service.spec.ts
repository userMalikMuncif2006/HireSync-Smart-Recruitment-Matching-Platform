import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AuthSessionService } from './auth-session.service';
import { LoginResponse } from './auth.models';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let session: AuthSessionService;
  let httpTesting: HttpTestingController;

  const loginResponse: LoginResponse = {
    accessToken: 'jwt-access-token',
    expiresAtUtc: '2099-01-01T00:00:00Z',
    userId: '11111111-1111-1111-1111-111111111111',
    email: 'employer@example.com',
    role: 'Employer',
  };

  beforeEach(() => {
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AuthService,
        AuthSessionService,
      ],
    });

    service = TestBed.inject(AuthService);
    session = TestBed.inject(AuthSessionService);
    httpTesting =
      TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
    sessionStorage.clear();
  });

  it('posts login credentials and stores the returned session', () => {
    service
      .login({
        email: 'employer@example.com',
        password: 'Password123!',
      })
      .subscribe();

    const request =
      httpTesting.expectOne('/api/v1/auth/login');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      email: 'employer@example.com',
      password: 'Password123!',
    });

    request.flush(loginResponse);

    expect(session.session()).toEqual(loginResponse);
    expect(session.hasRole('Employer')).toBe(true);
  });

  it('clears the session on logout', () => {
    session.setSession(loginResponse);

    service.logout();

    expect(session.session()).toBeNull();
    expect(session.isAuthenticated()).toBe(false);
  });
});