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
  EmployerOtpRequest,
  EmployerOtpVerifyRequest,
  EmployerOtpVerificationResponse,
  OtpRequestResponse,
} from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import {
  EmployerEmailVerificationPage,
} from './employer-email-verification-page';

describe('EmployerEmailVerificationPage', () => {
  let auth: FakeAuthService;
  let router: Router;
  let fixture:
    ComponentFixture<EmployerEmailVerificationPage>;
  let component: EmployerEmailVerificationPage;

  beforeEach(async () => {
    auth = new FakeAuthService();

    await TestBed.configureTestingModule({
      imports: [
        EmployerEmailVerificationPage,
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
                    ' employer@example.com ',
                }),
            },
          },
        },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);

    fixture =
      TestBed.createComponent(
        EmployerEmailVerificationPage,
      );

    component =
      fixture.componentInstance;
  });

  it('loads the Employer email and requests a verification code', () => {
    fixture.detectChanges();

    expect(component.email())
      .toBe('employer@example.com');

    expect(auth.otpRequests)
      .toEqual([
        {
          email: 'employer@example.com',
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
          email: 'employer@example.com',
          code: '123456',
        },
      ]);

    expect(navigateSpy)
      .toHaveBeenCalledWith(
        ['/login'],
        {
          queryParams: {
            verified: 'employer',
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
      Observable<EmployerOtpVerificationResponse> =
        of({
          succeeded: true,
          failureReason: null,
        });

    readonly otpRequests:
      EmployerOtpRequest[] = [];

    readonly verifyRequests:
      EmployerOtpVerifyRequest[] = [];

    requestEmployerOtp(
      request: EmployerOtpRequest,
    ): Observable<OtpRequestResponse> {
      this.otpRequests.push(request);
      return this.requestResult;
    }

    verifyEmployerOtp(
      request: EmployerOtpVerifyRequest,
    ): Observable<EmployerOtpVerificationResponse> {
      this.verifyRequests.push(request);
      return this.verifyResult;
    }
  }
});

