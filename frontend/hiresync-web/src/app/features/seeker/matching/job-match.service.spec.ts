import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { MatchResult } from './job-match.models';
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

    service = TestBed.inject(JobMatchService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('gets the authenticated Job Seeker match result', () => {
    const vacancyId =
      '11111111-1111-1111-1111-111111111111';

    const expected: MatchResult = {
      totalScore: 87.5,
      skillsScore: 37.5,
      experienceScore: 25,
      educationScore: 15,
      locationScore: 10,
      matchedSkills: [
        {
          id: '22222222-2222-2222-2222-222222222222',
          name: 'C#',
        },
      ],
      missingSkills: [
        {
          id: '33333333-3333-3333-3333-333333333333',
          name: 'Angular',
        },
      ],
    };

    let actual: MatchResult | undefined;

    service.getMatch(vacancyId).subscribe((result) => {
      actual = result;
    });

    const request =
      httpTesting.expectOne(
        `/api/v1/vacancies/${vacancyId}/match`,
      );

    expect(request.request.method).toBe('GET');

    request.flush(expected);

    expect(actual).toEqual(expected);
  });
});
