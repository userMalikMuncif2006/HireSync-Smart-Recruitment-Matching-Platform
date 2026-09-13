import {
  HttpErrorResponse,
} from '@angular/common/http';
import {
  Component,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import {
  Router,
} from '@angular/router';
import {
  finalize,
} from 'rxjs';

import {
  AuthService,
} from '../../../core/auth/auth.service';

type PasswordResetStage =
  | 'request'
  | 'complete';

const resendCooldownSeconds = 60;

@Component({
  selector:
    'app-forgot-password-page',
  imports: [
    ReactiveFormsModule,
  ],
  templateUrl:
    './forgot-password-page.html',
  styleUrl:
    './forgot-password-page.css',
})
export class ForgotPasswordPage {
  private readonly auth =
    inject(AuthService);

  private readonly router =
    inject(Router);

  private readonly destroyRef =
    inject(DestroyRef);

  private cooldownTimer:
    number | null = null;

  readonly stage =
    signal<PasswordResetStage>(
      'request',
    );

  readonly email =
    signal('');

  readonly isRequesting =
    signal(false);

  readonly isCompleting =
    signal(false);

  readonly cooldownSeconds =
    signal(0);

  readonly infoMessage =
    signal<string | null>(
      null,
    );

  readonly errorMessage =
    signal<string | null>(
      null,
    );

  readonly passwordError =
    signal<string | null>(
      null,
    );

  readonly passwordMismatch =
    signal(false);

  readonly emailControl =
    new FormControl(
      '',
      {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.email,
        ],
      },
    );

  readonly emailForm =
    new FormGroup({
      email:
        this.emailControl,
    });

  readonly codeControl =
    new FormControl(
      '',
      {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.pattern(
            /^\d{6}$/,
          ),
        ],
      },
    );

  readonly newPasswordControl =
    new FormControl(
      '',
      {
        nonNullable: true,
        validators: [
          Validators.required,
        ],
      },
    );

  readonly confirmPasswordControl =
    new FormControl(
      '',
      {
        nonNullable: true,
        validators: [
          Validators.required,
        ],
      },
    );

  readonly resetForm =
    new FormGroup({
      code:
        this.codeControl,
      newPassword:
        this.newPasswordControl,
      confirmPassword:
        this.confirmPasswordControl,
    });

  constructor() {
    this.destroyRef.onDestroy(
      () => {
        this.clearCooldownTimer();
      },
    );
  }

  requestCode(): void {
    this.errorMessage.set(
      null,
    );

    const email =
      this.emailControl.value
        .trim();

    this.emailControl.setValue(
      email,
    );

    if (
      this.emailForm.invalid
    ) {
      this.emailForm
        .markAllAsTouched();

      return;
    }

    this.email.set(
      email,
    );

    this.sendRequest(
      email,
      true,
    );
  }

  resendCode(): void {
    if (
      this.cooldownSeconds() > 0 ||
      this.isRequesting()
    ) {
      return;
    }

    const email =
      this.email();

    if (!email) {
      return;
    }

    this.sendRequest(
      email,
      false,
    );
  }

  completeReset(): void {
    this.errorMessage.set(
      null,
    );

    this.passwordError.set(
      null,
    );

    this.passwordMismatch.set(
      false,
    );

    const code =
      this.codeControl.value
        .trim();

    this.codeControl.setValue(
      code,
    );

    if (
      this.resetForm.invalid
    ) {
      this.resetForm
        .markAllAsTouched();

      return;
    }

    const newPassword =
      this.newPasswordControl
        .value;

    const confirmPassword =
      this.confirmPasswordControl
        .value;

    if (
      newPassword !==
      confirmPassword
    ) {
      this.passwordMismatch.set(
        true,
      );

      this.confirmPasswordControl
        .markAsTouched();

      return;
    }

    const email =
      this.email();

    if (!email) {
      this.errorMessage.set(
        'The password reset request is no longer available. Start again.',
      );

      return;
    }

    this.isCompleting.set(
      true,
    );

    this.auth
      .completePasswordReset({
        email,
        code,
        newPassword,
      })
      .pipe(
        finalize(() => {
          this.isCompleting.set(
            false,
          );
        }),
      )
      .subscribe({
        next: () => {
          void this.router.navigate(
            [
              '/login',
            ],
            {
              queryParams: {
                reset:
                  'password',
              },
            },
          );
        },

        error: (
          error:
            unknown,
        ) => {
          const passwordError =
            this.readPasswordError(
              error,
            );

          if (passwordError) {
            this.passwordError.set(
              passwordError,
            );

            return;
          }

          this.errorMessage.set(
            this.problemDetail(
              error,
              'The password could not be reset. Check the reset code and try again.',
            ),
          );
        },
      });
  }

  changeEmail(): void {
    this.clearCooldownTimer();

    this.cooldownSeconds.set(
      0,
    );

    this.stage.set(
      'request',
    );

    this.infoMessage.set(
      null,
    );

    this.errorMessage.set(
      null,
    );

    this.passwordError.set(
      null,
    );

    this.passwordMismatch.set(
      false,
    );

    this.resetForm.reset({
      code: '',
      newPassword: '',
      confirmPassword: '',
    });
  }

  backToLogin(): void {
    void this.router
      .navigateByUrl(
        '/login',
      );
  }

  private sendRequest(
    email: string,
    moveToCompleteStage:
      boolean,
  ): void {
    this.errorMessage.set(
      null,
    );

    this.isRequesting.set(
      true,
    );

    this.auth
      .requestPasswordReset({
        email,
      })
      .pipe(
        finalize(() => {
          this.isRequesting.set(
            false,
          );
        }),
      )
      .subscribe({
        next: (
          response,
        ) => {
          if (
            moveToCompleteStage
          ) {
            this.stage.set(
              'complete',
            );
          }

          this.infoMessage.set(
            response.message ||
            'If an account exists for this email, a reset code has been sent.',
          );

          this.startCooldown(
            resendCooldownSeconds,
          );
        },

        error: (
          error:
            unknown,
        ) => {
          this.errorMessage.set(
            this.problemDetail(
              error,
              'The password reset request could not be completed. Please try again.',
            ),
          );
        },
      });
  }

  private startCooldown(
    seconds: number,
  ): void {
    this.clearCooldownTimer();

    this.cooldownSeconds.set(
      seconds,
    );

    this.cooldownTimer =
      window.setInterval(
        () => {
          const next =
            this.cooldownSeconds() -
            1;

          if (next <= 0) {
            this.cooldownSeconds
              .set(0);

            this.clearCooldownTimer();

            return;
          }

          this.cooldownSeconds
            .set(next);
        },
        1000,
      );
  }

  private clearCooldownTimer(): void {
    if (
      this.cooldownTimer ===
      null
    ) {
      return;
    }

    window.clearInterval(
      this.cooldownTimer,
    );

    this.cooldownTimer =
      null;
  }

  private readPasswordError(
    error: unknown,
  ): string | null {
    if (
      !(error instanceof
        HttpErrorResponse)
    ) {
      return null;
    }

    const errors =
      error.error
        ?.errors
        ?.newPassword;

    if (
      !Array.isArray(errors)
    ) {
      return null;
    }

    const messages =
      errors.filter(
        (
          value:
            unknown,
        ): value is string =>
          typeof value ===
            'string' &&
          value.trim()
            .length > 0,
      );

    return messages.length > 0
      ? messages.join(' ')
      : null;
  }

  private problemDetail(
    error: unknown,
    fallback: string,
  ): string {
    if (
      error instanceof
        HttpErrorResponse &&
      typeof error.error
        ?.detail ===
        'string' &&
      error.error.detail
        .trim()
        .length > 0
    ) {
      return error.error
        .detail;
    }

    return fallback;
  }
}
