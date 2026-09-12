import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import {
  Component,
  DestroyRef,
  OnInit,
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
  ActivatedRoute,
  Router,
  RouterLink,
} from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-employer-email-verification-page',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl:
    './employer-email-verification-page.html',
  styleUrl:
    './employer-email-verification-page.css',
})
export class EmployerEmailVerificationPage
  implements OnInit
{
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  private cooldownTimer: number | null = null;

  readonly email = signal('');
  readonly isRequesting = signal(false);
  readonly isVerifying = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly infoMessage = signal<string | null>(null);
  readonly expiresAtUtc = signal<string | null>(null);
  readonly cooldownSeconds = signal(0);

  readonly code = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required],
  });

  readonly form = new FormGroup({
    code: this.code,
  });

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.clearCooldownTimer();
    });
  }

  ngOnInit(): void {
    const email =
      this.route.snapshot.queryParamMap
        .get('email')
        ?.trim() ?? '';

    this.email.set(email);

    if (!email) {
      this.errorMessage.set(
        'Employer email is required to continue verification.',
      );
      return;
    }

    this.requestCode();
  }

  requestCode(): void {
    const email = this.email();

    if (
      !email ||
      this.isRequesting() ||
      this.cooldownSeconds() > 0
    ) {
      return;
    }

    this.errorMessage.set(null);
    this.infoMessage.set(null);
    this.isRequesting.set(true);

    this.auth
      .requestEmployerOtp({ email })
      .pipe(
        finalize(() => {
          this.isRequesting.set(false);
        }),
      )
      .subscribe({
        next: (response) => {
          this.expiresAtUtc.set(
            response.expiresAtUtc,
          );

          this.infoMessage.set(
            'A verification code has been sent to your email address.',
          );
        },

        error: (error: unknown) => {
          if (error instanceof HttpErrorResponse) {
            const retryAfter =
              this.readRetryAfterSeconds(error);

            if (retryAfter > 0) {
              this.startCooldown(retryAfter);
            }
          }

          this.errorMessage.set(
            this.problemDetail(
              error,
              'The verification code could not be requested.',
            ),
          );
        },
      });
  }

  verify(): void {
    this.errorMessage.set(null);

    const value = this.code.value.trim();

    if (!value) {
      this.code.markAsTouched();
      return;
    }

    this.code.setValue(value);

    const email = this.email();

    if (!email) {
      this.errorMessage.set(
        'Employer email is required to continue verification.',
      );
      return;
    }

    this.isVerifying.set(true);

    this.auth
      .verifyEmployerOtp({
        email,
        code: value,
      })
      .pipe(
        finalize(() => {
          this.isVerifying.set(false);
        }),
      )
      .subscribe({
        next: () => {
          void this.router.navigate(
            ['/login'],
            {
              queryParams: {
                verified: 'employer',
              },
            },
          );
        },

        error: (error: unknown) => {
          this.errorMessage.set(
            this.problemDetail(
              error,
              'The verification code could not be verified.',
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

  private problemDetail(
    error: unknown,
    fallback: string,
  ): string {
    if (
      error instanceof HttpErrorResponse &&
      typeof error.error?.detail === 'string' &&
      error.error.detail.trim().length > 0
    ) {
      return error.error.detail;
    }

    return fallback;
  }
}

