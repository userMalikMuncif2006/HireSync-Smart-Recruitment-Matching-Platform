import { HttpErrorResponse } from '@angular/common/http';
import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  Router,
  provideRouter,
} from '@angular/router';
import {
  Observable,
  of,
  throwError,
} from 'rxjs';
import { vi } from 'vitest';

import {
  RegisterEmployerRequest,
  RegisterEmployerResponse,
  RegisterJobSeekerRequest,
  RegisterJobSeekerResponse,
} from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { RegisterPage } from './register-page';

describe('RegisterPage', () => {
  const jobSeekerResponse: RegisterJobSeekerResponse = {
    userId: '11111111-1111-1111-1111-111111111111',
    email: 'seeker@example.com',
    displayName: 'Test Seeker',
    role: 'JobSeeker',
  };

  const employerResponse: RegisterEmployerResponse = {
    userId: '22222222-2222-2222-2222-222222222222',
    employerProfileId:
      '33333333-3333-3333-3333-333333333333',
    email: 'company@example.com',
    role: 'Employer',
    employerVerificationStatus: 0,
  };

  let auth: FakeAuthService;
  let router: Router;
  let fixture: ComponentFixture<RegisterPage>;
  let component: RegisterPage;

  beforeEach(async () => {
    auth = new FakeAuthService();

    await TestBed.configureTestingModule({
      imports: [RegisterPage],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: auth,
        },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);

    fixture =
      TestBed.createComponent(RegisterPage);

    component =
      fixture.componentInstance;

    fixture.detectChanges();
  });

  it('starts as Job Seeker and renders the Job Seeker fields', () => {
    expect(component.form.controls.role.value)
      .toBe('JobSeeker');

    expect(component.isEmployer)
      .toBe(false);

    expect(
      fixture.nativeElement.querySelector(
        '#displayName',
      ),
    ).not.toBeNull();

    expect(
      fixture.nativeElement.querySelector(
        '#companyName',
      ),
    ).toBeNull();
  });

  it('switches to Employer fields and applies Employer validation', () => {
    component.form.controls.role
      .setValue('Employer');

    fixture.detectChanges();

    expect(component.isEmployer)
      .toBe(true);

    expect(
      fixture.nativeElement.querySelector(
        '#displayName',
      ),
    ).toBeNull();

    expect(
      fixture.nativeElement.querySelector(
        '#companyName',
      ),
    ).not.toBeNull();

    component.form.controls.description
      .setValue('Too short');

    expect(
      component.form.controls.description
        .hasError('minlength'),
    ).toBe(true);
  });

  it('blocks submission when passwords do not match', () => {
    component.form.patchValue({
      role: 'JobSeeker',
      email: 'seeker@example.com',
      password: 'Password123!',
      confirmPassword: 'DifferentPassword!',
      displayName: 'Test Seeker',
    });

    component.submit();

    expect(
      component.form.hasError(
        'passwordMismatch',
      ),
    ).toBe(true);

    expect(auth.jobSeekerRequests)
      .toHaveLength(0);

    expect(auth.employerRequests)
      .toHaveLength(0);
  });

  it('submits trimmed Job Seeker details and navigates to login', () => {
    const navigateSpy =
      vi.spyOn(router, 'navigate')
        .mockResolvedValue(true);

    component.form.patchValue({
      role: 'JobSeeker',
      email: '  seeker@example.com  ',
      password: ' Password123! ',
      confirmPassword: ' Password123! ',
      displayName: '  Test Seeker  ',
    });

    component.submit();

    expect(auth.jobSeekerRequests)
      .toEqual([
        {
          email: 'seeker@example.com',
          password: ' Password123! ',
          displayName: 'Test Seeker',
        },
      ]);

    expect(navigateSpy)
      .toHaveBeenCalledWith(
        ['/login'],
        {
          queryParams: {
            registered: 'jobseeker',
          },
        },
      );
  });

  it('submits the complete Employer registration contract', () => {
    component.form.controls.role
      .setValue('Employer');

    component.form.patchValue({
      email: '  company@example.com  ',
      password: 'Password123!',
      confirmPassword: 'Password123!',
      companyName: '  Example Company  ',
      description:
        '  A professional example company description.  ',
      location: '  Colombo  ',
      contactPersonName: '  Test Employer  ',
      contactPersonDesignation: '  HR Manager  ',
      businessRegistrationNumber: '  BR-1001  ',
      mobileNumber: '  0771234567  ',
      companyWebsite: '  https://example.com  ',
    });

    component.submit();

    expect(auth.employerRequests)
      .toEqual([
        {
          email: 'company@example.com',
          password: 'Password123!',
          companyName: 'Example Company',
          description:
            'A professional example company description.',
          location: 'Colombo',
          contactPersonName: 'Test Employer',
          contactPersonDesignation: 'HR Manager',
          businessRegistrationNumber: 'BR-1001',
          mobileNumber: '0771234567',
          companyWebsite: 'https://example.com',
        },
      ]);

    expect(
      component.registeredEmployerEmail(),
    ).toBe('company@example.com');

    expect(component.successMessage())
      .toContain(
        'Email verification is required',
      );
  });

  it('surfaces a backend registration conflict without clearing the form', () => {
    auth.jobSeekerResult =
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: {
              detail:
                'An account with this email already exists.',
            },
          }),
      );

    component.form.patchValue({
      role: 'JobSeeker',
      email: 'seeker@example.com',
      password: 'Password123!',
      confirmPassword: 'Password123!',
      displayName: 'Test Seeker',
    });

    component.submit();
    fixture.detectChanges();

    expect(component.errorMessage())
      .toBe(
        'An account with this email already exists.',
      );

    expect(component.form.controls.email.value)
      .toBe('seeker@example.com');

    expect(component.form.controls.displayName.value)
      .toBe('Test Seeker');

    expect(component.isSubmitting())
      .toBe(false);
  });

  class FakeAuthService {
    jobSeekerResult:
      Observable<RegisterJobSeekerResponse> =
        of(jobSeekerResponse);

    employerResult:
      Observable<RegisterEmployerResponse> =
        of(employerResponse);

    readonly jobSeekerRequests:
      RegisterJobSeekerRequest[] = [];

    readonly employerRequests:
      RegisterEmployerRequest[] = [];

    registerJobSeeker(
      request: RegisterJobSeekerRequest,
    ): Observable<RegisterJobSeekerResponse> {
      this.jobSeekerRequests.push(request);
      return this.jobSeekerResult;
    }

    registerEmployer(
      request: RegisterEmployerRequest,
    ): Observable<RegisterEmployerResponse> {
      this.employerRequests.push(request);
      return this.employerResult;
    }
  }
});

