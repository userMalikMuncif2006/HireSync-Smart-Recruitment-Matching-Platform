import {
  DatePipe,
} from '@angular/common';
import {
  HttpErrorResponse,
} from '@angular/common/http';
import {
  Component,
  computed,
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
  EducationLevel,
  JobSeekerCv,
  JobSeekerProfile,
  SkillSummary,
  UpdateJobSeekerProfileRequest,
} from './job-seeker-profile.models';
import {
  JobSeekerProfileService,
} from './job-seeker-profile.service';

@Component({
  selector:
    'app-job-seeker-profile-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DatePipe,
  ],
  templateUrl:
    './job-seeker-profile-page.html',
  styleUrl:
    './job-seeker-profile-page.css',
})
export class JobSeekerProfilePage {
  private readonly service =
    inject(JobSeekerProfileService);

  static readonly maxCvBytes =
    5_000_000;

  readonly educationOptions = [
    {
      value:
        EducationLevel
          .NoFormalQualification,
      label:
        'No formal qualification',
    },
    {
      value:
        EducationLevel.OrdinaryLevel,
      label:
        'Ordinary Level',
    },
    {
      value:
        EducationLevel.AdvancedLevel,
      label:
        'Advanced Level',
    },
    {
      value:
        EducationLevel.Certificate,
      label:
        'Certificate',
    },
    {
      value:
        EducationLevel.Diploma,
      label:
        'Diploma',
    },
    {
      value:
        EducationLevel.Bachelor,
      label:
        'Bachelor',
    },
    {
      value:
        EducationLevel
          .PostgraduateDiploma,
      label:
        'Postgraduate Diploma',
    },
    {
      value:
        EducationLevel.Master,
      label:
        'Master',
    },
    {
      value:
        EducationLevel.Doctorate,
      label:
        'Doctorate',
    },
  ] as const;

  readonly profile =
    signal<JobSeekerProfile | null>(
      null,
    );

  readonly cv =
    signal<JobSeekerCv | null>(
      null,
    );

  readonly availableSkills =
    signal<SkillSummary[]>([]);

  readonly selectedSkills =
    signal<SkillSummary[]>([]);

  readonly selectedSkillIds =
    computed(() =>
      this.selectedSkills().map(
        (skill) => skill.id,
      ),
    );

  readonly selectedCvFile =
    signal<File | null>(null);

  readonly isLoadingProfile =
    signal(false);

  readonly isSavingProfile =
    signal(false);

  readonly isLoadingSkills =
    signal(false);

  readonly isCvBusy =
    signal(false);

  readonly profileErrorMessage =
    signal<string | null>(null);

  readonly profileSuccessMessage =
    signal<string | null>(null);

  readonly skillErrorMessage =
    signal<string | null>(null);

  readonly cvErrorMessage =
    signal<string | null>(null);

  readonly cvSuccessMessage =
    signal<string | null>(null);

  readonly profileForm =
    new FormGroup({
      experienceMonths:
        new FormControl<
          number | null
        >(null, {
          validators: [
            Validators.required,
            Validators.min(0),
            Validators.max(720),
          ],
        }),
      educationLevel:
        new FormControl<
          EducationLevel | null
        >(null, {
          validators: [
            Validators.required,
          ],
        }),
      preferredLocation:
        new FormControl('', {
          nonNullable: true,
          validators: [
            Validators.required,
            Validators.maxLength(100),
          ],
        }),
    });

  readonly skillQueryControl =
    new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.maxLength(50),
      ],
    });

  constructor() {
    this.loadProfile();
    this.loadSkills();
    this.loadCv();
  }

  loadProfile(): void {
    this.isLoadingProfile.set(true);
    this.profileErrorMessage.set(null);

    this.service
      .getOwnProfile()
      .subscribe({
        next: (profile) => {
          this.applyProfile(profile);
          this.isLoadingProfile.set(false);
        },
        error: (
          error:
            HttpErrorResponse,
        ) => {
          if (error.status === 404) {
            this.profile.set(null);
            this.selectedSkills.set([]);

            this.profileForm.reset({
              experienceMonths: null,
              educationLevel: null,
              preferredLocation: '',
            });

            this.isLoadingProfile.set(false);
            return;
          }

          this.profileErrorMessage.set(
            this.problemMessage(
              error,
              'Your Job Seeker profile could not be loaded.',
            ),
          );

          this.isLoadingProfile.set(false);
        },
      });
  }

  loadSkills(
    query =
      this.skillQueryControl.value,
  ): void {
    if (this.skillQueryControl.invalid) {
      this.skillQueryControl
        .markAsTouched();

      return;
    }

    this.isLoadingSkills.set(true);
    this.skillErrorMessage.set(null);

    this.service
      .lookupSkills(query)
      .subscribe({
        next: (skills) => {
          this.availableSkills.set(
            skills,
          );

          this.isLoadingSkills.set(
            false,
          );
        },
        error: (
          error:
            HttpErrorResponse,
        ) => {
          this.skillErrorMessage.set(
            this.problemMessage(
              error,
              'Canonical skills could not be loaded.',
            ),
          );

          this.isLoadingSkills.set(
            false,
          );
        },
      });
  }

  clearSkillSearch(): void {
    this.skillQueryControl.setValue('');
    this.loadSkills('');
  }

  toggleSkill(
    skill: SkillSummary,
  ): void {
    const selected =
      this.selectedSkills();

    if (
      selected.some(
        (candidate) =>
          candidate.id === skill.id,
      )
    ) {
      this.selectedSkills.set(
        selected.filter(
          (candidate) =>
            candidate.id !== skill.id,
        ),
      );

      return;
    }

    this.selectedSkills.set([
      ...selected,
      skill,
    ]);
  }

  isSkillSelected(
    skillId: string,
  ): boolean {
    return this.selectedSkills()
      .some(
        (skill) =>
          skill.id === skillId,
      );
  }

  saveProfile(): void {
    this.profileErrorMessage.set(null);
    this.profileSuccessMessage.set(null);

    if (this.profileForm.invalid) {
      this.profileForm
        .markAllAsTouched();

      return;
    }

    const value =
      this.profileForm
        .getRawValue();

    if (
      value.experienceMonths ===
        null ||
      value.educationLevel === null
    ) {
      return;
    }

    const request:
      UpdateJobSeekerProfileRequest = {
        experienceMonths:
          value.experienceMonths,
        educationLevel:
          value.educationLevel,
        preferredLocation:
          value.preferredLocation
            .trim(),
        skillIds:
          this.selectedSkillIds(),
      };

    this.isSavingProfile.set(true);

    this.service
      .updateOwnProfile(request)
      .subscribe({
        next: (profile) => {
          this.applyProfile(profile);

          this.profileSuccessMessage.set(
            'Profile saved successfully.',
          );

          this.isSavingProfile.set(
            false,
          );
        },
        error: (
          error:
            HttpErrorResponse,
        ) => {
          this.profileErrorMessage.set(
            this.problemMessage(
              error,
              'Your Job Seeker profile could not be saved.',
            ),
          );

          this.isSavingProfile.set(
            false,
          );
        },
      });
  }

  loadCv(): void {
    this.cvErrorMessage.set(null);

    this.service
      .getOwnCv()
      .subscribe({
        next: (cv) => {
          this.cv.set(cv);
        },
        error: (
          error:
            HttpErrorResponse,
        ) => {
          if (error.status === 404) {
            this.cv.set(null);
            return;
          }

          this.cvErrorMessage.set(
            this.problemMessage(
              error,
              'Your CV details could not be loaded.',
            ),
          );
        },
      });
  }

  onCvSelected(
    event: Event,
  ): void {
    this.cvErrorMessage.set(null);
    this.cvSuccessMessage.set(null);
    this.selectedCvFile.set(null);

    const input =
      event.target as
        HTMLInputElement;

    const file =
      input.files?.item(0) ??
      null;

    if (!file) {
      return;
    }

    const lowerName =
      file.name.toLowerCase();

    const validExtension =
      lowerName.endsWith('.pdf') ||
      lowerName.endsWith('.docx');

    if (!validExtension) {
      this.cvErrorMessage.set(
        'Select a PDF or DOCX file.',
      );
      return;
    }

    if (file.size <= 0) {
      this.cvErrorMessage.set(
        'The CV file must not be empty.',
      );
      return;
    }

    if (
      file.size >
      JobSeekerProfilePage
        .maxCvBytes
    ) {
      this.cvErrorMessage.set(
        'The CV file must not exceed 5,000,000 bytes.',
      );
      return;
    }

    this.selectedCvFile.set(file);
  }

  uploadCv(): void {
    const file =
      this.selectedCvFile();

    this.cvErrorMessage.set(null);
    this.cvSuccessMessage.set(null);

    if (!file) {
      this.cvErrorMessage.set(
        'Select a valid PDF or DOCX CV first.',
      );
      return;
    }

    this.isCvBusy.set(true);

    this.service
      .uploadOwnCv(file)
      .subscribe({
        next: (cv) => {
          this.cv.set(cv);
          this.selectedCvFile.set(
            null,
          );

          this.cvSuccessMessage.set(
            'CV uploaded successfully.',
          );

          this.isCvBusy.set(false);
        },
        error: (
          error:
            HttpErrorResponse,
        ) => {
          this.cvErrorMessage.set(
            this.problemMessage(
              error,
              'Your CV could not be uploaded.',
            ),
          );

          this.isCvBusy.set(false);
        },
      });
  }

  downloadCv(): void {
    const cv =
      this.cv();

    if (!cv) {
      return;
    }

    this.cvErrorMessage.set(null);
    this.cvSuccessMessage.set(null);
    this.isCvBusy.set(true);

    this.service
      .downloadOwnCv()
      .subscribe({
        next: (blob) => {
          const url =
            URL.createObjectURL(blob);

          const anchor =
            document.createElement('a');

          anchor.href = url;
          anchor.download =
            cv.originalFileName;

          anchor.click();

          URL.revokeObjectURL(url);

          this.isCvBusy.set(false);
        },
        error: (
          error:
            HttpErrorResponse,
        ) => {
          this.cvErrorMessage.set(
            this.problemMessage(
              error,
              'Your protected CV could not be downloaded.',
            ),
          );

          this.isCvBusy.set(false);
        },
      });
  }

  formatBytes(
    bytes: number,
  ): string {
    if (bytes < 1_000) {
      return `${bytes} B`;
    }

    if (bytes < 1_000_000) {
      return `${
        (bytes / 1_000)
          .toFixed(1)
      } KB`;
    }

    return `${
      (bytes / 1_000_000)
        .toFixed(2)
    } MB`;
  }

  private applyProfile(
    profile: JobSeekerProfile,
  ): void {
    this.profile.set(profile);

    this.selectedSkills.set([
      ...profile.skills,
    ]);

    this.profileForm.setValue({
      experienceMonths:
        profile
          .totalExperienceMonths,
      educationLevel:
        profile.educationLevel,
      preferredLocation:
        profile
          .preferredLocation ??
        '',
    });
  }

  private problemMessage(
    error: HttpErrorResponse,
    fallback: string,
  ): string {
    const body =
      error.error as
        | {
            detail?: unknown;
          }
        | null;

    if (
      body &&
      typeof body.detail ===
        'string' &&
      body.detail.trim()
        .length > 0
    ) {
      return body.detail;
    }

    return fallback;
  }
}
