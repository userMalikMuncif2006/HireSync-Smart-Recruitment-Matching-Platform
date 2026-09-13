import {
  HttpErrorResponse,
  HttpHeaders,
} from '@angular/common/http';
import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  ActivatedRoute,
  convertToParamMap,
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
  JobSeekerOtpRequest,
  JobSeekerOtpVerifyRequest,
  JobSeekerOtpVerificationResponse,
  OtpRequestResponse,
} from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import {
  JobSeekerEmailVerificationPage,
} from './jobseeker-email-verification-page';

describe('JobSeekerEmailVerificationPage', () => {
  let auth: FakeAuthService;
  let router: Router;
  let fixture:
    ComponentFixture<JobSeekerEmailVerificationPage>;
  let component: JobSeekerEmailVerificationPage;

  beforeEach(async () => {
    auth = new FakeAuthService();

    await TestBed.configureTestingModule({
      imports: [
        JobSeekerEmailVerificationPage,
      ],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: auth,
        },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap:
                convertToParamMap({
                  email:
                    ' jobseeker@example.com ',
                }),
            },
          },
        },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);

    fixture =
      TestBed.createComponent(
        JobSeekerEmailVerificationPage,
      );

    component =
      fixture.componentInstance;
  });

  it('loads the JobSeeker email and requests a verification code', () => {
    fixture.detectChanges();

    expect(component.email())
      .toBe('jobseeker@example.com');

    expect(auth.otpRequests)
      .toEqual([
        {
          email: 'jobseeker@example.com',
        },
      ]);

    expect(component.expiresAtUtc())
      .toBe('2099-01-01T00:05:00Z');

    expect(component.infoMessage())
      .toContain(
        'verification code has been sent',
      );
  });

  it('prevents the browser native form submission', () => {
    fixture.detectChanges();

    const form =
      fixture.nativeElement.querySelector(
        'form',
      ) as HTMLFormElement;

    const event =
      new Event('submit', {
        bubbles: true,
        cancelable: true,
      });

    form.dispatchEvent(event);

    expect(event.defaultPrevented)
      .toBe(true);
  });
  it('requires a verification code before submitting', () => {
    fixture.detectChanges();

    component.code.setValue('   ');
    component.verify();

    expect(auth.verifyRequests)
      .toHaveLength(0);

    expect(component.code.touched)
      .toBe(true);
  });

  it('trims and verifies the code then navigates to login', () => {
    const navigateSpy =
      vi.spyOn(router, 'navigate')
        .mockResolvedValue(true);

    fixture.detectChanges();

    component.code.setValue('  123456  ');
    component.verify();

    expect(auth.verifyRequests)
      .toEqual([
        {
          email: 'jobseeker@example.com',
          code: '123456',
        },
      ]);

    expect(navigateSpy)
      .toHaveBeenCalledWith(
        ['/login'],
        {
          queryParams: {
            verified: 'jobseeker',
          },
        },
      );
  });

  it('surfaces backend verification problem details', () => {
    auth.verifyResult =
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              detail:
                'The verification code has expired.',
            },
          }),
      );

    fixture.detectChanges();

    component.code.setValue('123456');
    component.verify();

    expect(component.errorMessage())
      .toBe(
        'The verification code has expired.',
      );

    expect(component.isVerifying())
      .toBe(false);
  });

  it('uses Retry-After to activate resend cooldown', () => {
    auth.requestResult =
      throwError(
        () =>
          new HttpErrorResponse({
            status: 429,
            headers: new HttpHeaders({
              'Retry-After': '30',
            }),
            error: {
              detail:
                'Please wait before requesting another verification code.',
            },
          }),
      );

    fixture.detectChanges();

    expect(component.cooldownSeconds())
      .toBe(30);

    expect(component.errorMessage())
      .toBe(
        'Please wait before requesting another verification code.',
      );
  });

  class FakeAuthService {
    requestResult:
      Observable<OtpRequestResponse> =
        of({
          succeeded: true,
          expiresAtUtc:
            '2099-01-01T00:05:00Z',
          failureReason: null,
          retryAfterSeconds: null,
        });

    verifyResult:
      Observable<JobSeekerOtpVerificationResponse> =
        of({
          succeeded: true,
          failureReason: null,
        });

    readonly otpRequests:
      JobSeekerOtpRequest[] = [];

    readonly verifyRequests:
      JobSeekerOtpVerifyRequest[] = [];

    requestJobSeekerOtp(
      request: JobSeekerOtpRequest,
    ): Observable<OtpRequestResponse> {
      this.otpRequests.push(request);
      return this.requestResult;
    }

    verifyJobSeekerOtp(
      request: JobSeekerOtpVerifyRequest,
    ): Observable<JobSeekerOtpVerificationResponse> {
      this.verifyRequests.push(request);
      return this.verifyResult;
    }
  }
});
