import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  inject,
  signal,
} from '@angular/core';
import {
  takeUntilDestroyed,
} from '@angular/core/rxjs-interop';
import {
  FormsModule,
  NgForm,
} from '@angular/forms';
import {
  ActivatedRoute,
  ParamMap,
  Router,
} from '@angular/router';
import { finalize } from 'rxjs';

import {
  AuthRole,
  LoginResponse,
} from '../../../core/auth/auth.models';
import {
  AuthService,
} from '../../../core/auth/auth.service';
import {
  AutoFocusDirective,
} from '../../../shared/directives/auto-focus.directive';

const roleHome: Record<AuthRole, string> = {
  JobSeeker: '/seeker',
  Employer: '/employer',
  Administrator: '/admin',
};

@Component({
  selector: 'app-login-page',
  imports: [
    FormsModule,
    AutoFocusDirective,
  ],
  templateUrl: './login-page.html',
  styleUrl: './login-page.css',
})
export class LoginPage {
  private readonly auth =
    inject(AuthService);

  private readonly router =
    inject(Router);

  private readonly route =
    inject(ActivatedRoute);

  readonly isSubmitting =
    signal(false);

  readonly errorMessage =
    signal<string | null>(null);

  readonly successMessage =
    signal<string | null>(null);

  readonly credentials = {
    email: '',
    password: '',
  };

  constructor() {
    this.route.queryParamMap
      .pipe(
        takeUntilDestroyed(),
      )
      .subscribe(
        (params) => {
          this.successMessage.set(
            this.onboardingMessage(
              params,
            ),
          );
        },
      );
  }

  goToRegister(): void {
    void this.router.navigateByUrl(
      '/register',
    );
  }
  submit(
    form: NgForm,
  ): void {
    this.errorMessage.set(null);

    const email =
      this.credentials.email.trim();

    if (
      email !==
      this.credentials.email
    ) {
      this.credentials.email =
        email;
    }

    // Keep the template-driven controls authoritative while
    // making programmatic submission deterministic as well.
    form.controls['email']
      ?.setValue(email);

    form.controls['password']
      ?.setValue(
        this.credentials.password,
      );

    if (form.invalid) {
      Object
        .values(form.controls)
        .forEach(
          (control) =>
            control.markAsTouched(),
        );

      return;
    }

    const password =
      this.credentials.password;

    this.isSubmitting.set(true);

    this.auth
      .login({
        email,
        password,
      })
      .pipe(
        finalize(() => {
          this.isSubmitting.set(
            false,
          );
        }),
      )
      .subscribe({
        next: (response) => {
          this.redirectAfterLogin(
            response,
          );
        },

        error: (error: unknown) => {
          this.errorMessage.set(
            this.loginErrorMessage(
              error,
            ),
          );
        },
      });
  }

  private onboardingMessage(
    params: ParamMap,
  ): string | null {
    if (
      params.get('registered') ===
      'jobseeker'
    ) {
      return 'Job Seeker account created successfully. You can now sign in.';
    }

    if (
      params.get('verified') ===
      'employer'
    ) {
      return 'Employer email verified successfully. Your account remains subject to Administrator approval before sign in is available.';
    }

    if (
      params.get('activated') ===
      'administrator'
    ) {
      return 'Administrator activation completed successfully. You can now sign in.';
    }

    return null;
  }

  private redirectAfterLogin(
    response: LoginResponse,
  ): void {
    void this.router.navigateByUrl(
      roleHome[response.role],
    );
  }

  private loginErrorMessage(
    error: unknown,
  ): string {
    if (
      error instanceof
        HttpErrorResponse &&
      error.status === 401
    ) {
      return 'The email or password is incorrect.';
    }

    if (
      error instanceof
        HttpErrorResponse &&
      error.status === 403
    ) {
      return 'Sign in is not available for this account.';
    }

    return 'Sign in could not be completed. Please try again.';
  }
}