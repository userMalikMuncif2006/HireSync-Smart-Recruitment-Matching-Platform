import {
  HttpErrorResponse,
  HttpHeaders,
} from '@angular/common/http';
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
  AdministratorActivationRequest,
  AdministratorActivationRequestResponse,
  AdministratorActivationVerificationResponse,
  AdministratorActivationVerifyRequest,
} from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import {
  AdministratorActivationPage,
} from './administrator-activation-page';

describe('AdministratorActivationPage', () => {
  let auth: FakeAuthService;
  let router: Router;
  let fixture:
    ComponentFixture<AdministratorActivationPage>;
  let component: AdministratorActivationPage;

  beforeEach(async () => {
    auth = new FakeAuthService();

    await TestBed.configureTestingModule({
      imports: [
        AdministratorActivationPage,
      ],
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
      TestBed.createComponent(
        AdministratorActivationPage,
      );

    component =
      fixture.componentInstance;

    fixture.detectChanges();
  });

  it('starts in credential-entry mode', () => {
    expect(component.activationRequested())
      .toBe(false);

    expect(
      fixture.nativeElement.querySelector(
        '#activationCode',
      ),
    ).toBeNull();

    expect(
      fixture.nativeElement.textContent,
    ).toContain(
      'Send activation code',
    );
  });

  it('requires Administrator email and password before requesting a code', () => {
    component.requestCode();

    expect(auth.requestCalls)
      .toHaveLength(0);

    expect(
      component.form.controls.email.touched,
    ).toBe(true);

    expect(
      component.form.controls.password.touched,
    ).toBe(true);
  });

  it('trims email but preserves password when requesting activation OTP', () => {
    component.form.controls.email
      .setValue(' admin@example.com ');

    component.form.controls.password
      .setValue('  Secret Password  ');

    component.requestCode();

    expect(auth.requestCalls)
      .toEqual([
        {
          email: 'admin@example.com',
          password: '  Secret Password  ',
        },
      ]);

    expect(component.activationRequested())
      .toBe(true);

    expect(component.expiresAtUtc())
      .toBe('2099-01-01T00:05:00Z');

    expect(component.infoMessage())
      .toContain(
        'activation code has been sent',
      );
  });

  it('prevents native browser form submission', () => {
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

  it('uses Retry-After for activation resend cooldown', () => {
    auth.requestResult =
      throwError(
        () =>
          new HttpErrorResponse({
            status: 429,
            headers: new HttpHeaders({
              'Retry-After': '25',
            }),
            error: {
              detail:
                'Please wait before requesting another activation code.',
            },
          }),
      );

    component.form.controls.email
      .setValue('admin@example.com');

    component.form.controls.password
      .setValue('SecretPassword');

    component.requestCode();

    expect(component.activationRequested())
      .toBe(true);

    expect(component.cooldownSeconds())
      .toBe(25);

    expect(component.errorMessage())
      .toBe(
        'Please wait before requesting another activation code.',
      );
  });

  it('verifies activation without exposing credentials in navigation', () => {
    const navigateSpy =
      vi.spyOn(router, 'navigate')
        .mockResolvedValue(true);

    component.form.controls.email
      .setValue('admin@example.com');

    component.form.controls.password
      .setValue('SecretPassword');

    component.requestCode();

    component.form.controls.code
      .setValue('  123456  ');

    component.verify();

    expect(auth.verifyCalls)
      .toEqual([
        {
          email: 'admin@example.com',
          password: 'SecretPassword',
          code: '123456',
        },
      ]);

    expect(navigateSpy)
      .toHaveBeenCalledWith(
        ['/login'],
        {
          queryParams: {
            activated: 'administrator',
          },
        },
      );

    expect(
      component.form.controls.password.value,
    ).toBe('');

    expect(
      component.form.controls.code.value,
    ).toBe('');
  });

  it('surfaces backend activation failure title when detail is absent', () => {
    auth.verifyResult =
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              title:
                'Invalid activation code',
            },
          }),
      );

    component.form.controls.email
      .setValue('admin@example.com');

    component.form.controls.password
      .setValue('SecretPassword');

    component.requestCode();

    component.form.controls.code
      .setValue('123456');

    component.verify();

    expect(component.errorMessage())
      .toBe(
        'Invalid activation code',
      );

    expect(component.isVerifying())
      .toBe(false);
  });

  class FakeAuthService {
    requestResult:
      Observable<AdministratorActivationRequestResponse> =
        of({
          succeeded: true,
          expiresAtUtc:
            '2099-01-01T00:05:00Z',
          failureReason: null,
          retryAfterSeconds: null,
        });

    verifyResult:
      Observable<AdministratorActivationVerificationResponse> =
        of({
          succeeded: true,
          failureReason: null,
        });

    readonly requestCalls:
      AdministratorActivationRequest[] = [];

    readonly verifyCalls:
      AdministratorActivationVerifyRequest[] = [];

    requestAdministratorActivationOtp(
      request: AdministratorActivationRequest,
    ): Observable<AdministratorActivationRequestResponse> {
      this.requestCalls.push(request);
      return this.requestResult;
    }

    verifyAdministratorActivationOtp(
      request: AdministratorActivationVerifyRequest,
    ): Observable<AdministratorActivationVerificationResponse> {
      this.verifyCalls.push(request);
      return this.verifyResult;
    }
  }
});
