import { Component, inject, signal } from '@angular/core';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';

import {
  EmployerProfile,
  EmployerVerificationStatus,
  UpdateEmployerProfileRequest,
} from './employer-profile.models';
import { EmployerProfileService } from './employer-profile.service';

@Component({
  selector: 'app-employer-profile-page',
  imports: [ReactiveFormsModule],
  templateUrl: './employer-profile-page.html',
  styleUrl: './employer-profile-page.css',
})
export class EmployerProfilePage {
  private readonly employerProfileService =
    inject(EmployerProfileService);

  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly profile = signal<EmployerProfile | null>(null);

  readonly form = new FormGroup({
    companyName: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(150),
      ],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(20),
        Validators.maxLength(2000),
      ],
    }),
    location: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(100),
      ],
    }),
    contactPersonName: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(100),
      ],
    }),
    contactPersonDesignation: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(100),
      ],
    }),
    businessRegistrationNumber: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(100),
      ],
    }),
    mobileNumber: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(5),
        Validators.maxLength(30),
      ],
    }),
    companyWebsite: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(300)],
    }),
  });

  constructor() {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.employerProfileService.getOwnProfile().subscribe({
      next: (profile) => {
        this.profile.set(profile);

        this.form.setValue({
          companyName: profile.companyName,
          description: profile.description,
          location: profile.location,
          contactPersonName: profile.contactPersonName,
          contactPersonDesignation:
            profile.contactPersonDesignation,
          businessRegistrationNumber:
            profile.businessRegistrationNumber,
          mobileNumber: profile.mobileNumber,
          companyWebsite: profile.companyWebsite ?? '',
        });

        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set(
          'Employer profile could not be loaded.',
        );
        this.isLoading.set(false);
      },
    });
  }

  save(): void {
    this.successMessage.set(null);
    this.errorMessage.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    const request: UpdateEmployerProfileRequest = {
      companyName: value.companyName.trim(),
      description: value.description.trim(),
      location: value.location.trim(),
      contactPersonName: value.contactPersonName.trim(),
      contactPersonDesignation:
        value.contactPersonDesignation.trim(),
      businessRegistrationNumber:
        value.businessRegistrationNumber.trim(),
      mobileNumber: value.mobileNumber.trim(),
      companyWebsite:
        value.companyWebsite.trim().length === 0
          ? null
          : value.companyWebsite.trim(),
    };

    this.isSaving.set(true);

    this.employerProfileService
      .updateOwnProfile(request)
      .subscribe({
        next: (profile) => {
          this.profile.set(profile);
          this.successMessage.set(
            'Employer profile updated successfully.',
          );
          this.isSaving.set(false);
        },
        error: () => {
          this.errorMessage.set(
            'Employer profile could not be updated.',
          );
          this.isSaving.set(false);
        },
      });
  }

  verificationStatusLabel(): string {
    switch (this.profile()?.employerVerificationStatus) {
      case EmployerVerificationStatus.Approved:
        return 'Approved';
      case EmployerVerificationStatus.Rejected:
        return 'Rejected';
      case EmployerVerificationStatus.Pending:
        return 'Pending';
      default:
        return 'Unknown';
    }
  }
}