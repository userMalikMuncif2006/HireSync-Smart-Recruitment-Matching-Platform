import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  Observable,
  of,
} from 'rxjs';

import {
  EducationLevel,
  JobSeekerCv,
  JobSeekerProfile,
  SkillSummary,
  UpdateJobSeekerProfileRequest,
} from './job-seeker-profile.models';
import {
  JobSeekerProfilePage,
} from './job-seeker-profile-page';
import {
  JobSeekerProfileService,
} from './job-seeker-profile.service';

describe(
  'JobSeekerProfilePage',
  () => {
    let fixture:
      ComponentFixture<
        JobSeekerProfilePage
      >;

    let service:
      FakeJobSeekerProfileService;

    const angularSkill:
      SkillSummary = {
        id:
          '11111111-1111-1111-1111-111111111111',
        name:
          'Angular',
      };

    const dotnetSkill:
      SkillSummary = {
        id:
          '22222222-2222-2222-2222-222222222222',
        name:
          '.NET',
      };

    const profile:
      JobSeekerProfile = {
        totalExperienceMonths: 18,
        educationLevel:
          EducationLevel.Bachelor,
        preferredLocation:
          'Colombo',
        skills: [
          angularSkill,
        ],
        isMatchReady: false,
      };

    async function createComponent():
      Promise<void> {
      service =
        new FakeJobSeekerProfileService();

      await TestBed
        .configureTestingModule({
          imports: [
            JobSeekerProfilePage,
          ],
          providers: [
            {
              provide:
                JobSeekerProfileService,
              useValue:
                service,
            },
          ],
        })
        .compileComponents();

      fixture =
        TestBed.createComponent(
          JobSeekerProfilePage,
        );

      fixture.detectChanges();

      await fixture.whenStable();

      fixture.detectChanges();
    }

    afterEach(() => {
      vi.restoreAllMocks();
    });

    it('renders server match readiness instead of calculating it locally', async () => {
      await createComponent();

      const text =
        fixture.nativeElement
          .textContent as string;

      expect(text)
        .toContain(
          'Not match ready',
        );

      expect(
        fixture.componentInstance
          .profileForm
          .getRawValue(),
      ).toEqual({
        experienceMonths: 18,
        educationLevel:
          EducationLevel.Bachelor,
        preferredLocation:
          'Colombo',
      });
    });

    it('saves canonical skill IDs only', async () => {
      await createComponent();

      fixture.componentInstance
        .toggleSkill(
          dotnetSkill,
        );

      fixture.componentInstance
        .profileForm
        .setValue({
          experienceMonths: 24,
          educationLevel:
            EducationLevel.Master,
          preferredLocation:
            ' Kandy ',
        });

      fixture.componentInstance
        .saveProfile();

      expect(
        service.updateRequests,
      ).toEqual([
        {
          experienceMonths: 24,
          educationLevel:
            EducationLevel.Master,
          preferredLocation:
            'Kandy',
          skillIds: [
            angularSkill.id,
            dotnetSkill.id,
          ],
        },
      ]);
    });

    it('accepts an exact 5,000,000-byte PDF for client-side upload UX', async () => {
      await createComponent();

      const file =
        new File(
          [
            new Uint8Array(
              5_000_000,
            ),
          ],
          'cv.pdf',
          {
            type:
              'application/pdf',
          },
        );

      fixture.componentInstance
        .onCvSelected(
          {
            target: {
              files: {
                item:
                  () => file,
              },
            },
          } as unknown as Event,
        );

      expect(
        fixture.componentInstance
          .selectedCvFile(),
      ).toBe(file);

      fixture.componentInstance
        .uploadCv();

      expect(
        service.uploadedFiles,
      ).toEqual([file]);
    });

    it('rejects a CV larger than the exact client UX boundary', async () => {
      await createComponent();

      const file =
        new File(
          ['x'],
          'too-large.pdf',
          {
            type:
              'application/pdf',
          },
        );

      Object.defineProperty(
        file,
        'size',
        {
          value:
            5_000_001,
        },
      );

      fixture.componentInstance
        .onCvSelected(
          {
            target: {
              files: {
                item:
                  () => file,
              },
            },
          } as unknown as Event,
        );

      expect(
        fixture.componentInstance
          .selectedCvFile(),
      ).toBeNull();

      expect(
        fixture.componentInstance
          .cvErrorMessage(),
      ).toContain(
        '5,000,000',
      );
    });
  },
);

class FakeJobSeekerProfileService {
  readonly updateRequests:
    UpdateJobSeekerProfileRequest[] =
      [];

  readonly uploadedFiles:
    File[] =
      [];

  private readonly skills:
    SkillSummary[] = [
      {
        id:
          '11111111-1111-1111-1111-111111111111',
        name:
          'Angular',
      },
      {
        id:
          '22222222-2222-2222-2222-222222222222',
        name:
          '.NET',
      },
    ];

  private readonly profile:
    JobSeekerProfile = {
      totalExperienceMonths: 18,
      educationLevel:
        EducationLevel.Bachelor,
      preferredLocation:
        'Colombo',
      skills: [
        this.skills[0],
      ],
      isMatchReady: false,
    };

  private readonly cv:
    JobSeekerCv = {
      originalFileName:
        'existing.pdf',
      extension:
        '.pdf',
      contentType:
        'application/pdf',
      sizeBytes:
        1_200,
      uploadedAtUtc:
        '2026-09-12T10:00:00Z',
    };

  getOwnProfile():
    Observable<JobSeekerProfile> {
    return of(
      this.profile,
    );
  }

  updateOwnProfile(
    request:
      UpdateJobSeekerProfileRequest,
  ): Observable<JobSeekerProfile> {
    this.updateRequests.push(
      request,
    );

    return of({
      ...this.profile,
      totalExperienceMonths:
        request.experienceMonths,
      educationLevel:
        request.educationLevel,
      preferredLocation:
        request.preferredLocation,
      skills:
        this.skills.filter(
          (skill) =>
            request.skillIds
              .includes(skill.id),
        ),
      isMatchReady:
        true,
    });
  }

  lookupSkills():
    Observable<SkillSummary[]> {
    return of(
      this.skills,
    );
  }

  getOwnCv():
    Observable<JobSeekerCv> {
    return of(
      this.cv,
    );
  }

  uploadOwnCv(
    file: File,
  ): Observable<JobSeekerCv> {
    this.uploadedFiles.push(
      file,
    );

    return of({
      ...this.cv,
      originalFileName:
        file.name,
      sizeBytes:
        file.size,
    });
  }

  downloadOwnCv():
    Observable<Blob> {
    return of(
      new Blob(['cv']),
    );
  }
}
