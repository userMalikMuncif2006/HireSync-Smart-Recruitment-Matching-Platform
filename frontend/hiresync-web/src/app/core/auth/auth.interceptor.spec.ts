import {
  HttpClient,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { authInterceptor } from './auth.interceptor';
import { AuthSessionService } from './auth-session.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let session: AuthSessionService;

  beforeEach(() => {
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(
          withInterceptors([authInterceptor]),
        ),
        provideHttpClientTesting(),
        AuthSessionService,
      ],
    });

    http = TestBed.inject(HttpClient);
    httpTesting =
      TestBed.inject(HttpTestingController);
    session =
      TestBed.inject(AuthSessionService);
  });

  afterEach(() => {
    httpTesting.verify();
    sessionStorage.clear();
  });

  it('adds the bearer token to protected API requests', () => {
    session.setSession({
      accessToken: 'test-access-token',
      expiresAtUtc: '2099-01-01T00:00:00Z',
      userId: '11111111-1111-1111-1111-111111111111',
      email: 'employer@example.com',
      role: 'Employer',
    });

    http.get('/api/v1/employer/profile').subscribe();

    const request =
      httpTesting.expectOne(
        '/api/v1/employer/profile',
      );

    expect(
      request.request.headers.get('Authorization'),
    ).toBe('Bearer test-access-token');

    request.flush({});
  });

  it('does not add authorization when no session exists', () => {
    http.get('/api/v1/employer/profile').subscribe();

    const request =
      httpTesting.expectOne(
        '/api/v1/employer/profile',
      );

    expect(
      request.request.headers.has('Authorization'),
    ).toBe(false);

    request.flush({});
  });

  it('does not add authorization to authentication requests', () => {
    session.setSession({
      accessToken: 'existing-token',
      expiresAtUtc: '2099-01-01T00:00:00Z',
      userId: '11111111-1111-1111-1111-111111111111',
      email: 'employer@example.com',
      role: 'Employer',
    });

    http.post('/api/v1/auth/login', {}).subscribe();

    const request =
      httpTesting.expectOne('/api/v1/auth/login');

    expect(
      request.request.headers.has('Authorization'),
    ).toBe(false);

    request.flush({});
  });
});