import {
  HttpErrorResponse,
} from '@angular/common/http';
import { DatePipe } from '@angular/common';
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
  RouterLink,
} from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-administrator-activation-page',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl:
    './administrator-activation-page.html',
  styleUrl:
    './administrator-activation-page.css',
})
export class AdministratorActivationPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  private cooldownTimer: number | null = null;

  readonly activationRequested = signal(false);
  readonly isRequesting = signal(false);
  readonly isVerifying = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly infoMessage = signal<string | null>(null);
  readonly expiresAtUtc = signal<string | null>(null);
  readonly cooldownSeconds = signal(0);

  readonly form = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.email,
      ],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    code: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.clearCooldownTimer();
    });
  }

  submit(): void {
    if (this.activationRequested()) {
      this.verify();
      return;
    }

    this.requestCode();
  }

  requestCode(): void {
    if (
      this.isRequesting() ||
      this.isVerifying() ||
      this.cooldownSeconds() > 0
    ) {
      return;
    }

    const emailControl = this.form.controls.email;
    const passwordControl = this.form.controls.password;

    const email = emailControl.value.trim();
    const password = passwordControl.value;

    if (emailControl.value !== email) {
      emailControl.setValue(email);
    }

    emailControl.markAsTouched();
    passwordControl.markAsTouched();

    if (
      emailControl.invalid ||
      passwordControl.invalid
    ) {
      return;
    }

    this.errorMessage.set(null);
    this.infoMessage.set(null);
    this.isRequesting.set(true);

    this.auth
      .requestAdministratorActivationOtp({
        email,
        password,
      })
      .pipe(
        finalize(() => {
          this.isRequesting.set(false);
        }),
      )
      .subscribe({
        next: (response) => {
          this.activationRequested.set(true);
          this.expiresAtUtc.set(
            response.expiresAtUtc,
          );

          this.infoMessage.set(
            'An Administrator activation code has been sent to the configured email address.',
          );
        },

        error: (error: unknown) => {
          if (
            error instanceof HttpErrorResponse &&
            error.status === 429
          ) {
            this.activationRequested.set(true);

            const retryAfter =
              this.readRetryAfterSeconds(error);

            if (retryAfter > 0) {
              this.startCooldown(retryAfter);
            }
          }

          this.errorMessage.set(
            this.problemMessage(
              error,
              'The Administrator activation code could not be requested.',
            ),
          );
        },
      });
  }

  verify(): void {
    if (
      this.isVerifying() ||
      this.isRequesting()
    ) {
      return;
    }

    const email = this.form.controls.email.value.trim();
    const password = this.form.controls.password.value;
    const codeControl = this.form.controls.code;
    const code = codeControl.value.trim();

    if (!code) {
      codeControl.markAsTouched();
      return;
    }

    codeControl.setValue(code);

    this.errorMessage.set(null);
    this.isVerifying.set(true);

    this.auth
      .verifyAdministratorActivationOtp({
        email,
        password,
        code,
      })
      .pipe(
        finalize(() => {
          this.isVerifying.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.form.controls.password.setValue('');
          this.form.controls.code.setValue('');

          void this.router.navigate(
            ['/login'],
            {
              queryParams: {
                activated: 'administrator',
              },
            },
          );
        },

        error: (error: unknown) => {
          this.errorMessage.set(
            this.problemMessage(
              error,
              'Administrator activation could not be completed.',
            ),
          );
        },
      });
  }

  private readRetryAfterSeconds(
    error: HttpErrorResponse,
  ): number {
    const header =
      error.headers.get('Retry-After');

    if (!header) {
      return 0;
    }

    const seconds = Number(header);

    if (
      !Number.isFinite(seconds) ||
      seconds <= 0
    ) {
      return 0;
    }

    return Math.ceil(seconds);
  }

  private startCooldown(seconds: number): void {
    this.clearCooldownTimer();
    this.cooldownSeconds.set(seconds);

    this.cooldownTimer = window.setInterval(
      () => {
        const next =
          this.cooldownSeconds() - 1;

        if (next <= 0) {
          this.cooldownSeconds.set(0);
          this.clearCooldownTimer();
          return;
        }

        this.cooldownSeconds.set(next);
      },
      1000,
    );
  }

  private clearCooldownTimer(): void {
    if (this.cooldownTimer === null) {
      return;
    }

    window.clearInterval(
      this.cooldownTimer,
    );

    this.cooldownTimer = null;
  }

  private problemMessage(
    error: unknown,
    fallback: string,
  ): string {
    if (!(error instanceof HttpErrorResponse)) {
      return fallback;
    }

    if (
      typeof error.error?.detail === 'string' &&
      error.error.detail.trim().length > 0
    ) {
      return error.error.detail;
    }

    if (
      typeof error.error?.title === 'string' &&
      error.error.title.trim().length > 0
    ) {
      return error.error.title;
    }

    return fallback;
  }
}
