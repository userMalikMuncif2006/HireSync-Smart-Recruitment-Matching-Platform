import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
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
} from '@angular/router';

import {
  CreateVacancyPayload,
  EducationLevel,
  SkillSummary,
  UpdateVacancyPayload,
} from './employer-vacancy.models';
import { EmployerVacancyService } from './employer-vacancy.service';

@Component({
  selector: 'app-employer-vacancy-form-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
  ],
  templateUrl: './employer-vacancy-form-page.html',
  styleUrl: './employer-vacancy-form-page.css',
})
export class EmployerVacancyFormPage {
  private readonly service =
    inject(EmployerVacancyService);

  private readonly route =
    inject(ActivatedRoute);

  private readonly router =
    inject(Router);

  private readonly vacancyId =
    this.route.snapshot.paramMap.get(
      'vacancyId',
    );

  private rowVersion:
    string | null = null;

  readonly isEditMode =
    this.vacancyId !== null;

  readonly isClosed =
    signal(false);

  readonly isLoading =
    signal(false);

  readonly isSaving =
    signal(false);

  readonly errorMessage =
    signal<string | null>(null);

  readonly skillError =
    signal<string | null>(null);

  readonly isLoadingSkills =
    signal(false);

  readonly availableSkills =
    signal<SkillSummary[]>([]);

  readonly selectedSkills =
    signal<SkillSummary[]>([]);

  readonly skillQuery =
    new FormControl(
      '',
      {
        nonNullable: true,
        validators: [
          Validators.maxLength(50),
        ],
      },
    );

  readonly form =
    new FormGroup({
      title:
        new FormControl(
          '',
          {
            nonNullable: true,
            validators: [
              Validators.required,
              Validators.minLength(3),
              Validators.maxLength(150),
            ],
          },
        ),

      description:
        new FormControl(
          '',
          {
            nonNullable: true,
            validators: [
              Validators.required,
              Validators.minLength(20),
              Validators.maxLength(5000),
            ],
          },
        ),

      location:
        new FormControl(
          '',
          {
            nonNullable: true,
            validators: [
              Validators.required,
              Validators.minLength(2),
              Validators.maxLength(100),
            ],
          },
        ),

      minimumExperienceMonths:
        new FormControl(
          0,
          {
            nonNullable: true,
            validators: [
              Validators.required,
              Validators.min(0),
              Validators.max(720),
            ],
          },
        ),

      requiredEducationLevel:
        new FormControl<
          EducationLevel | null
        >(null),
    });

  constructor() {
    this.searchSkills();

    if (this.vacancyId) {
      this.loadVacancy(
        this.vacancyId,
      );
    }
  }

  searchSkills(): void {
    if (this.skillQuery.invalid) {
      this.skillQuery.markAsTouched();
      return;
    }

    this.isLoadingSkills.set(true);
    this.skillError.set(null);

    this.service
      .getSkills(
        this.skillQuery.value,
      )
      .subscribe({
        next: (skills) => {
          this.availableSkills.set(
            skills,
          );

          this.isLoadingSkills.set(false);
        },
        error: (error: unknown) => {
          this.skillError.set(
            this.readError(
              error,
              'Skills could not be loaded.',
            ),
          );

          this.isLoadingSkills.set(false);
        },
      });
  }

  clearSkillSearch(): void {
    this.skillQuery.setValue('');
    this.searchSkills();
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

  toggleSkill(
    skill: SkillSummary,
  ): void {
    if (this.isClosed()) {
      return;
    }

    if (
      this.isSkillSelected(
        skill.id,
      )
    ) {
      this.removeSkill(
        skill.id,
      );

      return;
    }

    if (
      this.selectedSkills().length >=
      50
    ) {
      this.skillError.set(
        'A vacancy can require at most 50 skills.',
      );

      return;
    }

    this.skillError.set(null);

    this.selectedSkills.update(
      (current) => [
        ...current,
        skill,
      ],
    );
  }

  removeSkill(
    skillId: string,
  ): void {
    if (this.isClosed()) {
      return;
    }

    this.selectedSkills.update(
      (current) =>
        current.filter(
          (skill) =>
            skill.id !== skillId,
        ),
    );
  }

  save(): void {
    this.errorMessage.set(null);

    if (this.isClosed()) {
      this.errorMessage.set(
        'A Closed vacancy is read-only.',
      );
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const selectedSkillIds =
      this.selectedSkills()
        .map(
          (skill) =>
            skill.id,
        );

    if (
      selectedSkillIds.length < 1 ||
      selectedSkillIds.length > 50
    ) {
      this.skillError.set(
        'Select at least one and at most 50 required skills.',
      );

      return;
    }

    const value =
      this.form.getRawValue();

    const payload:
      CreateVacancyPayload = {
        title:
          value.title.trim(),

        description:
          value.description.trim(),

        location:
          value.location.trim(),

        minimumExperienceMonths:
          value.minimumExperienceMonths,

        requiredEducationLevel:
          value.requiredEducationLevel,

        requiredSkillIds:
          selectedSkillIds,
      };

    this.isSaving.set(true);

    if (this.isEditMode) {
      this.updateVacancy(
        payload,
      );

      return;
    }

    this.service
      .createVacancy(payload)
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.backToVacancies();
        },
        error: (error: unknown) => {
          this.errorMessage.set(
            this.readError(
              error,
              'Vacancy could not be created.',
            ),
          );

          this.isSaving.set(false);
        },
      });
  }

  backToVacancies(): void {
    void this.router.navigate([
      '/employer/vacancies',
    ]);
  }

  private loadVacancy(
    vacancyId: string,
  ): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.service
      .getVacancy(
        vacancyId,
      )
      .subscribe({
        next: (vacancy) => {
          this.rowVersion =
            vacancy.rowVersion;

          this.isClosed.set(
            vacancy.status === 2,
          );

          this.form.setValue({
            title:
              vacancy.title,
            description:
              vacancy.description,
            location:
              vacancy.location,
            minimumExperienceMonths:
              vacancy.minimumExperienceMonths,
            requiredEducationLevel:
              vacancy.requiredEducationLevel,
          });

          this.selectedSkills.set(
            [...vacancy.requiredSkills],
          );

          if (vacancy.status === 2) {
            this.form.disable();
          }

          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.errorMessage.set(
            this.readError(
              error,
              'Vacancy could not be loaded.',
            ),
          );

          this.isLoading.set(false);
        },
      });
  }

  private updateVacancy(
    payload: CreateVacancyPayload,
  ): void {
    if (
      !this.vacancyId ||
      !this.rowVersion
    ) {
      this.errorMessage.set(
        'The vacancy concurrency state is unavailable. Reload and try again.',
      );

      this.isSaving.set(false);
      return;
    }

    const update:
      UpdateVacancyPayload = {
        ...payload,
        rowVersion:
          this.rowVersion,
      };

    this.service
      .updateVacancy(
        this.vacancyId,
        update,
      )
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.backToVacancies();
        },
        error: (error: unknown) => {
          this.errorMessage.set(
            this.readError(
              error,
              'Vacancy could not be updated.',
            ),
          );

          this.isSaving.set(false);
        },
      });
  }

  private readError(
    error: unknown,
    fallback: string,
  ): string {
    if (
      error instanceof
      HttpErrorResponse
    ) {
      const detail =
        error.error?.detail;

      if (
        typeof detail === 'string' &&
        detail.trim().length > 0
      ) {
        return detail;
      }
    }

    return fallback;
  }
}