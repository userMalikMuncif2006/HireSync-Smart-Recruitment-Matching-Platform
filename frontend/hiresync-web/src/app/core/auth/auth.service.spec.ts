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

  it('posts Job Seeker registration', () => {
    const body = {
      email: 'seeker@example.com',
      password: 'Password123!',
      displayName: 'Test Seeker',
    };

    service.registerJobSeeker(body).subscribe();

    const request = httpTesting.expectOne(
      '/api/v1/auth/register/jobseeker',
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);

    request.flush({
      userId: '22222222-2222-2222-2222-222222222222',
      email: body.email,
      displayName: body.displayName,
      role: 'JobSeeker',
    });
  });

  it('posts Employer registration', () => {
    const body = {
      email: 'company@example.com',
      password: 'Password123!',
      companyName: 'Example Company',
      description:
        'A professional example company description.',
      location: 'Colombo',
      contactPersonName: 'Test Employer',
      contactPersonDesignation: 'Manager',
      businessRegistrationNumber: 'BR-1001',
      mobileNumber: '0771234567',
      companyWebsite: 'https://example.com',
    };

    service.registerEmployer(body).subscribe();

    const request = httpTesting.expectOne(
      '/api/v1/auth/register/employer',
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);

    request.flush({
      userId: '33333333-3333-3333-3333-333333333333',
      employerProfileId:
        '44444444-4444-4444-4444-444444444444',
      email: body.email,
      role: 'Employer',
      employerVerificationStatus: 0,
    });
  });

  it('requests a Job Seeker OTP', () => {
    service
      .requestJobSeekerOtp({
        email: 'seeker@example.com',
      })
      .subscribe();

    const request = httpTesting.expectOne(
      '/api/v1/auth/jobseeker/otp/request',
    );

    expect(request.request.method).toBe('POST');

    expect(request.request.body).toEqual({
      email: 'seeker@example.com',
    });

    request.flush({
      succeeded: true,
      expiresAtUtc: '2099-01-01T00:05:00Z',
      failureReason: null,
      retryAfterSeconds: null,
    });
  });

  it('verifies a Job Seeker OTP', () => {
    service
      .verifyJobSeekerOtp({
        email: 'seeker@example.com',
        code: '123456',
      })
      .subscribe();

    const request = httpTesting.expectOne(
      '/api/v1/auth/jobseeker/otp/verify',
    );

    expect(request.request.method).toBe('POST');

    expect(request.request.body).toEqual({
      email: 'seeker@example.com',
      code: '123456',
    });

    request.flush({
      succeeded: true,
      failureReason: null,
    });
  });
  it('requests an Employer OTP', () => {
    service
      .requestEmployerOtp({
        email: 'company@example.com',
      })
      .subscribe();

    const request = httpTesting.expectOne(
      '/api/v1/auth/employer/otp/request',
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      email: 'company@example.com',
    });

    request.flush({
      succeeded: true,
      expiresAtUtc: '2099-01-01T00:05:00Z',
      failureReason: null,
      retryAfterSeconds: null,
    });
  });

  it('verifies an Employer OTP', () => {
    service
      .verifyEmployerOtp({
        email: 'company@example.com',
        code: '123456',
      })
      .subscribe();

    const request = httpTesting.expectOne(
      '/api/v1/auth/employer/otp/verify',
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      email: 'company@example.com',
      code: '123456',
    });

    request.flush({
      succeeded: true,
      failureReason: null,
    });
  });

  it('requests Administrator first activation OTP', () => {
    service
      .requestAdministratorActivationOtp({
        email: 'admin@example.com',
        password: 'Password123!',
      })
      .subscribe();

    const request = httpTesting.expectOne(
      '/api/v1/auth/admin/activation/otp/request',
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      email: 'admin@example.com',
      password: 'Password123!',
    });

    request.flush({
      succeeded: true,
      expiresAtUtc: '2099-01-01T00:05:00Z',
      failureReason: null,
      retryAfterSeconds: null,
    });
  });

  it('verifies Administrator first activation OTP', () => {
    service
      .verifyAdministratorActivationOtp({
        email: 'admin@example.com',
        password: 'Password123!',
        code: '123456',
      })
      .subscribe();

    const request = httpTesting.expectOne(
      '/api/v1/auth/admin/activation/otp/verify',
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      email: 'admin@example.com',
      password: 'Password123!',
      code: '123456',
    });

    request.flush({
      succeeded: true,
      failureReason: null,
    });
  });

  it('clears the session on logout', () => {
    session.setSession(loginResponse);

    service.logout();

    expect(session.session()).toBeNull();
    expect(session.isAuthenticated()).toBe(false);
  });
});
