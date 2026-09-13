import {
  HttpErrorResponse,
} from '@angular/common/http';
import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  Router,
} from '@angular/router';
import {
  Observable,
  of,
  throwError,
} from 'rxjs';

import {
  PasswordResetCompleteRequest,
  PasswordResetCompleteResponse,
  PasswordResetRequest,
  PasswordResetRequestResponse,
} from '../../../core/auth/auth.models';
import {
  AuthService,
} from '../../../core/auth/auth.service';
import {
  ForgotPasswordPage,
} from './forgot-password-page';

describe(
  'ForgotPasswordPage',
  () => {
    let auth:
      FakeAuthService;

    let router:
      FakeRouter;

    let fixture:
      ComponentFixture<
        ForgotPasswordPage
      >;

    let component:
      ForgotPasswordPage;

    beforeEach(
      async () => {
        auth =
          new FakeAuthService();

        router =
          new FakeRouter();

        await TestBed
          .configureTestingModule({
            imports: [
              ForgotPasswordPage,
            ],
            providers: [
              {
                provide:
                  AuthService,
                useValue:
                  auth,
              },
              {
                provide:
                  Router,
                useValue:
                  router,
              },
            ],
          })
          .compileComponents();

        fixture =
          TestBed.createComponent(
            ForgotPasswordPage,
          );

        component =
          fixture.componentInstance;

        fixture.detectChanges();
      },
    );

    afterEach(() => {
      fixture.destroy();
    });

    it('uses reactive forms and starts at email request', () => {
      expect(
        component.stage(),
      ).toBe('request');

      expect(
        fixture.nativeElement
          .querySelector(
            '[formcontrolname="email"]',
          ),
      ).not.toBeNull();
    });

    it('submits trimmed email and shows generic confirmation', () => {
      component.emailControl
        .setValue(
          '  user@example.com  ',
        );

      component.requestCode();

      expect(
        auth.requestRequests,
      ).toEqual([
        {
          email:
            'user@example.com',
        },
      ]);

      expect(
        component.stage(),
      ).toBe('complete');

      expect(
        component.cooldownSeconds(),
      ).toBe(60);

      expect(
        component.infoMessage(),
      ).toContain(
        'If an account exists',
      );
    });

    it('rejects invalid email before request', () => {
      component.emailControl
        .setValue(
          'not-an-email',
        );

      component.requestCode();

      expect(
        auth.requestRequests,
      ).toHaveLength(0);
    });

    it('blocks mismatched confirmation password', () => {
      component.emailControl
        .setValue(
          'user@example.com',
        );

      component.requestCode();

      component.codeControl
        .setValue(
          '123456',
        );

      component.newPasswordControl
        .setValue(
          'NewValidPassword456!',
        );

      component.confirmPasswordControl
        .setValue(
          'DifferentPassword456!',
        );

      component.completeReset();

      expect(
        component.passwordMismatch(),
      ).toBe(true);

      expect(
        auth.completeRequests,
      ).toHaveLength(0);
    });

    it('completes reset and navigates to login success state', () => {
      component.emailControl
        .setValue(
          'user@example.com',
        );

      component.requestCode();

      component.codeControl
        .setValue(
          '123456',
        );

      component.newPasswordControl
        .setValue(
          'NewValidPassword456!',
        );

      component.confirmPasswordControl
        .setValue(
          'NewValidPassword456!',
        );

      component.completeReset();

      expect(
        auth.completeRequests,
      ).toEqual([
        {
          email:
            'user@example.com',
          code:
            '123456',
          newPassword:
            'NewValidPassword456!',
        },
      ]);

      expect(
        router.navigations,
      ).toEqual([
        {
          commands: [
            '/login',
          ],
          queryParams: {
            reset:
              'password',
          },
        },
      ]);
    });

    it('shows server password validation errors', () => {
      auth.completeResult =
        throwError(
          () =>
            new HttpErrorResponse({
              status: 400,
              error: {
                errors: {
                  newPassword: [
                    'Passwords must contain a non alphanumeric character.',
                  ],
                },
              },
            }),
        );

      component.emailControl
        .setValue(
          'user@example.com',
        );

      component.requestCode();

      component.codeControl
        .setValue(
          '123456',
        );

      component.newPasswordControl
        .setValue(
          'Password123',
        );

      component.confirmPasswordControl
        .setValue(
          'Password123',
        );

      component.completeReset();

      expect(
        component.passwordError(),
      ).toContain(
        'non alphanumeric',
      );
    });

    class FakeAuthService {
      requestResult:
        Observable<
          PasswordResetRequestResponse
        > =
          of({
            message:
              'If an account exists for this email, a reset code has been sent.',
          });

      completeResult:
        Observable<
          PasswordResetCompleteResponse
        > =
          of({
            message:
              'Password reset successfully.',
          });

      readonly requestRequests:
        PasswordResetRequest[] = [];

      readonly completeRequests:
        PasswordResetCompleteRequest[] = [];

      requestPasswordReset(
        request:
          PasswordResetRequest,
      ): Observable<
        PasswordResetRequestResponse
      > {
        this.requestRequests.push(
          request,
        );

        return this.requestResult;
      }

      completePasswordReset(
        request:
          PasswordResetCompleteRequest,
      ): Observable<
        PasswordResetCompleteResponse
      > {
        this.completeRequests.push(
          request,
        );

        return this.completeResult;
      }
    }

    class FakeRouter {
      readonly navigations:
        Array<{
          commands:
            string[];
          queryParams:
            Record<
              string,
              string
            >;
        }> = [];

      readonly destinations:
        string[] = [];

      navigate(
        commands:
          string[],
        extras: {
          queryParams:
            Record<
              string,
              string
            >;
        },
      ): Promise<boolean> {
        this.navigations.push({
          commands,
          queryParams:
            extras.queryParams,
        });

        return Promise.resolve(
          true,
        );
      }

      navigateByUrl(
        url:
          string,
      ): Promise<boolean> {
        this.destinations.push(
          url,
        );

        return Promise.resolve(
          true,
        );
      }
    }
  },
);
