import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import {
  PublicVacancyDetail,
} from './job-match.models';
import { JobMatchService } from './job-match.service';

describe('JobMatchService', () => {
  let service: JobMatchService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        JobMatchService,
      ],
    });

    service =
      TestBed.inject(JobMatchService);

    httpTesting =
      TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('gets the canonical authenticated vacancy detail', () => {
    const vacancyId =
      '11111111-1111-1111-1111-111111111111';

    const expected: PublicVacancyDetail = {
      id: vacancyId,
      title: 'Backend Developer',
      description: 'Build secure services.',
      companyName: 'HireSync Employer',
      companyDescription:
        'Recruitment company',
      companyWebsite: null,
      companyLocation: 'Colombo',
      location: 'Colombo',
      minimumExperienceMonths: 12,
      requiredEducationLevel: 2,
      publishedAtUtc:
        '2026-09-12T09:00:00Z',
      requiredSkills: [],
      matchStatus: 'Ready',
      match: {
        totalScore: 87.5,
        skillsScore: 37.5,
        experienceScore: 25,
        educationScore: 15,
        locationScore: 10,
        matchedSkills: [],
        missingSkills: [],
      },
      missingProfileFields: [],
      canApply: true,
      hasApplied: false,
      computedAtUtc:
        '2026-09-12T12:30:00Z',
    };

    let actual:
      PublicVacancyDetail | undefined;

    service
      .getVacancyDetail(vacancyId)
      .subscribe((result) => {
        actual = result;
      });

    const request =
      httpTesting.expectOne(
        `/api/v1/vacancies/${vacancyId}`,
      );

    expect(request.request.method)
      .toBe('GET');

    request.flush(expected);

    expect(actual)
      .toEqual(expected);
  });
});