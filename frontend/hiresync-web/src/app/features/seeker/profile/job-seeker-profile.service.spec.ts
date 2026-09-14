import {
  provideHttpClient,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import {
  TestBed,
} from '@angular/core/testing';

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

describe(
  'JobSeekerProfileService',
  () => {
    let service:
      JobSeekerProfileService;

    let httpTesting:
      HttpTestingController;

    const skills:
      SkillSummary[] = [
        {
          id:
            '11111111-1111-1111-1111-111111111111',
          name: 'Angular',
        },
      ];

    const profile:
      JobSeekerProfile = {
        totalExperienceMonths: 24,
        educationLevel:
          EducationLevel.Bachelor,
        preferredLocation:
          'Colombo',
        skills,
        isMatchReady: true,
      };

    const cv:
      JobSeekerCv = {
        originalFileName:
          'vimaltan-cv.pdf',
        extension: '.pdf',
        contentType:
          'application/pdf',
        sizeBytes: 1200,
        uploadedAtUtc:
          '2026-09-12T10:00:00Z',
      };

    beforeEach(() => {
      TestBed.configureTestingModule({
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          JobSeekerProfileService,
        ],
      });

      service =
        TestBed.inject(
          JobSeekerProfileService,
        );

      httpTesting =
        TestBed.inject(
          HttpTestingController,
        );
    });

    afterEach(() => {
      httpTesting.verify();
    });

    it('loads the authenticated Job Seeker profile', () => {
      let actual:
        JobSeekerProfile | undefined;

      service
        .getOwnProfile()
        .subscribe((result) => {
          actual = result;
        });

      const request =
        httpTesting.expectOne(
          '/api/v1/job-seeker/profile',
        );

      expect(
        request.request.method,
      ).toBe('GET');

      request.flush(profile);

      expect(actual)
        .toEqual(profile);
    });

    it('updates the profile with canonical skill IDs', () => {
      const update:
        UpdateJobSeekerProfileRequest = {
          experienceMonths: 24,
          educationLevel:
            EducationLevel.Bachelor,
          preferredLocation:
            'Colombo',
          skillIds:
            skills.map(
              (skill) => skill.id,
            ),
        };

      service
        .updateOwnProfile(update)
        .subscribe();

      const request =
        httpTesting.expectOne(
          '/api/v1/job-seeker/profile',
        );

      expect(
        request.request.method,
      ).toBe('PUT');

      expect(
        request.request.body,
      ).toEqual(update);

      request.flush(profile);
    });

    it('uses the shared canonical skill lookup contract', () => {
      service
        .lookupSkills(
          '  ang  ',
        )
        .subscribe();

      const request =
        httpTesting.expectOne(
          (candidate) =>
            candidate.url ===
              '/api/v1/skills' &&
            candidate.params.get(
              'query',
            ) === 'ang',
        );

      expect(
        request.request.method,
      ).toBe('GET');

      request.flush(skills);
    });

    it('loads current CV metadata', () => {
      service
        .getOwnCv()
        .subscribe();

      const request =
        httpTesting.expectOne(
          '/api/v1/job-seeker/cv',
        );

      expect(
        request.request.method,
      ).toBe('GET');

      request.flush(cv);
    });

    it('uploads exactly one multipart field named file', () => {
      const file =
        new File(
          ['pdf'],
          'cv.pdf',
          {
            type:
              'application/pdf',
          },
        );

      service
        .uploadOwnCv(file)
        .subscribe();

      const request =
        httpTesting.expectOne(
          '/api/v1/job-seeker/cv',
        );

      expect(
        request.request.method,
      ).toBe('POST');

      expect(
        request.request.body,
      ).toBeInstanceOf(FormData);

      const body =
        request.request.body as
          FormData;

      expect(
        Array.from(body.keys()),
      ).toEqual(['file']);

      expect(
        body.get('file'),
      ).toBe(file);

      request.flush(cv);
    });

    it('downloads the protected owner CV as a blob', () => {
      let actual:
        Blob | undefined;

      service
        .downloadOwnCv()
        .subscribe((result) => {
          actual = result;
        });

      const request =
        httpTesting.expectOne(
          '/api/v1/job-seeker/cv/file',
        );

      expect(
        request.request.method,
      ).toBe('GET');

      expect(
        request.request.responseType,
      ).toBe('blob');

      const blob =
        new Blob(
          ['protected-cv'],
          {
            type:
              'application/pdf',
          },
        );

      request.flush(blob);

      expect(actual)
        .toBeInstanceOf(Blob);
    });
  },
);
