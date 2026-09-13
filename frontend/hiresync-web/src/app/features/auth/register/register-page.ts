import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';

type RegistrationRole = 'JobSeeker' | 'Employer';

const passwordsMatchValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const password = control.get('password')?.value;
  const confirmPassword =
    control.get('confirmPassword')?.value;

  if (!password || !confirmPassword) {
    return null;
  }

  return password === confirmPassword
    ? null
    : { passwordMismatch: true };
};

const optionalHttpUrlValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const value = String(control.value ?? '').trim();

  if (!value) {
    return null;
  }

  try {
    const url = new URL(value);

    return url.protocol === 'http:' ||
      url.protocol === 'https:'
      ? null
      : { httpUrl: true };
  } catch {
    return { httpUrl: true };
  }
};

@Component({
  selector: 'app-register-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './register-page.html',
  styleUrls: [
    './register-page.css',
    './register-form.css',
    './register-visual.css',
    './register-responsive.css',
  ],
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly registeredEmployerEmail =
    signal<string | null>(null);

  readonly form = new FormGroup(
    {
      role: new FormControl<RegistrationRole>(
        'JobSeeker',
        {
          nonNullable: true,
          validators: [Validators.required],
        },
      ),

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

      confirmPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),

      displayName: new FormControl('', {
        nonNullable: true,
      }),

      companyName: new FormControl('', {
        nonNullable: true,
      }),

      description: new FormControl('', {
        nonNullable: true,
      }),

      location: new FormControl('', {
        nonNullable: true,
      }),

      contactPersonName: new FormControl('', {
        nonNullable: true,
      }),

      contactPersonDesignation: new FormControl('', {
        nonNullable: true,
      }),

      businessRegistrationNumber:
        new FormControl('', {
          nonNullable: true,
        }),

      mobileNumber: new FormControl('', {
        nonNullable: true,
      }),

      companyWebsite: new FormControl('', {
        nonNullable: true,
      }),
    },
    {
      validators: [passwordsMatchValidator],
    },
  );

  constructor() {
    this.applyRoleValidators(
      this.form.controls.role.value,
    );

    this.form.controls.role.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((role) => {
        this.errorMessage.set(null);
        this.successMessage.set(null);
        this.registeredEmployerEmail.set(null);

        this.applyRoleValidators(role);
      });
  }

  get isEmployer(): boolean {
    return this.form.controls.role.value === 'Employer';
  }

  submit(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.trimTextControls();

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);

    if (this.isEmployer) {
      this.submitEmployer();
      return;
    }

    this.submitJobSeeker();
  }

  private submitJobSeeker(): void {
    this.auth
      .registerJobSeeker({
        email: this.form.controls.email.value,
        password:
          this.form.controls.password.value,
        displayName:
          this.form.controls.displayName.value,
      })
      .pipe(
        finalize(() => {
          this.isSubmitting.set(false);
        }),
      )
      .subscribe({
        next: () => {
          void this.router.navigate(
            ['/login'],
            {
              queryParams: {
                registered: 'jobseeker',
              },
            },
          );
        },
        error: (error: unknown) => {
          this.errorMessage.set(
            this.registrationErrorMessage(error),
          );
        },
      });
  }

  private submitEmployer(): void {
    const website =
      this.form.controls.companyWebsite.value;

    this.auth
      .registerEmployer({
        email: this.form.controls.email.value,
        password:
          this.form.controls.password.value,
        companyName:
          this.form.controls.companyName.value,
        description:
          this.form.controls.description.value,
        location:
          this.form.controls.location.value,
        contactPersonName:
          this.form.controls.contactPersonName.value,
        contactPersonDesignation:
          this.form.controls
            .contactPersonDesignation.value,
        businessRegistrationNumber:
          this.form.controls
            .businessRegistrationNumber.value,
        mobileNumber:
          this.form.controls.mobileNumber.value,
        companyWebsite:
          website.length > 0
            ? website
            : null,
      })
      .pipe(
        finalize(() => {
          this.isSubmitting.set(false);
        }),
      )
      .subscribe({
        next: (response) => {
          this.registeredEmployerEmail.set(
            response.email,
          );

          this.successMessage.set(
            'Employer account created successfully. Email verification is required before sign in.',
          );

          void this.router.navigate(
            ['/verify-employer-email'],
            {
              queryParams: {
                email: response.email,
              },
            },
          );
        },
        error: (error: unknown) => {
          this.errorMessage.set(
            this.registrationErrorMessage(error),
          );
        },
      });
  }

  private applyRoleValidators(
    role: RegistrationRole,
  ): void {
    const displayName =
      this.form.controls.displayName;

    const employerControls = [
      this.form.controls.companyName,
      this.form.controls.description,
      this.form.controls.location,
      this.form.controls.contactPersonName,
      this.form.controls.contactPersonDesignation,
      this.form.controls.businessRegistrationNumber,
      this.form.controls.mobileNumber,
    ];

    if (role === 'JobSeeker') {
      displayName.setValidators([
        Validators.required,
        Validators.maxLength(100),
      ]);

      for (const control of employerControls) {
        control.clearValidators();
      }

      this.form.controls.companyWebsite
        .clearValidators();
    } else {
      displayName.clearValidators();

      this.form.controls.companyName.setValidators([
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(150),
      ]);

      this.form.controls.description.setValidators([
        Validators.required,
        Validators.minLength(20),
        Validators.maxLength(2000),
      ]);

      this.form.controls.location.setValidators([
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(100),
      ]);

      this.form.controls.contactPersonName
        .setValidators([
          Validators.required,
          Validators.minLength(2),
          Validators.maxLength(100),
        ]);

      this.form.controls.contactPersonDesignation
        .setValidators([
          Validators.required,
          Validators.minLength(2),
          Validators.maxLength(100),
        ]);

      this.form.controls
        .businessRegistrationNumber
        .setValidators([
          Validators.required,
          Validators.minLength(2),
          Validators.maxLength(100),
        ]);

      this.form.controls.mobileNumber
        .setValidators([
          Validators.required,
          Validators.minLength(5),
          Validators.maxLength(30),
        ]);

      this.form.controls.companyWebsite
        .setValidators([
          Validators.maxLength(300),
          optionalHttpUrlValidator,
        ]);
    }

    displayName.updateValueAndValidity({
      emitEvent: false,
    });

    for (const control of employerControls) {
      control.updateValueAndValidity({
        emitEvent: false,
      });
    }

    this.form.controls.companyWebsite
      .updateValueAndValidity({
        emitEvent: false,
      });
  }

  private trimTextControls(): void {
    const controls = [
      this.form.controls.email,
      this.form.controls.displayName,
      this.form.controls.companyName,
      this.form.controls.description,
      this.form.controls.location,
      this.form.controls.contactPersonName,
      this.form.controls.contactPersonDesignation,
      this.form.controls.businessRegistrationNumber,
      this.form.controls.mobileNumber,
      this.form.controls.companyWebsite,
    ];

    for (const control of controls) {
      const trimmed = control.value.trim();

      if (control.value !== trimmed) {
        control.setValue(trimmed);
      }
    }
  }

  private registrationErrorMessage(
    error: unknown,
  ): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 409) {
        return (
          error.error?.detail ??
          'An account with these details already exists.'
        );
      }

      if (error.status === 400) {
        return (
          error.error?.detail ??
          'Please review the registration details and try again.'
        );
      }
    }

    return 'Registration could not be completed. Please try again.';
  }
}

