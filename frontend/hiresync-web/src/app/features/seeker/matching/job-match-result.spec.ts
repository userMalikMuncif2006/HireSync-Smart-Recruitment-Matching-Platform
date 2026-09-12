import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { MatchResult } from './job-match.models';
import { JobMatchResult } from './job-match-result';
import { JobMatchService } from './job-match.service';

describe('JobMatchResult', () => {
  let fixture: ComponentFixture<JobMatchResult>;
  let service: FakeJobMatchService;

  const match: MatchResult = {
    totalScore: 87.5,
    skillsScore: 37.5,
    experienceScore: 25,
    educationScore: 15,
    locationScore: 10,
    matchedSkills: [
      {
        id: '11111111-1111-1111-1111-111111111111',
        name: 'C#',
      },
    ],
    missingSkills: [
      {
        id: '22222222-2222-2222-2222-222222222222',
        name: 'Angular',
      },
    ],
  };

  beforeEach(async () => {
    service = new FakeJobMatchService();

    await TestBed.configureTestingModule({
      imports: [JobMatchResult],
      providers: [
        {
          provide: JobMatchService,
          useValue: service,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(JobMatchResult);
  });

  it('renders backend total and component scores unchanged', async () => {
    service.response = of(match);

    fixture.componentRef.setInput(
      'vacancyId',
      '33333333-3333-3333-3333-333333333333',
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text =
      fixture.nativeElement.textContent as string;

    expect(text).toContain('87.5%');
    expect(text).toContain('37.5 / 50');
    expect(text).toContain('25 / 25');
    expect(text).toContain('15 / 15');
    expect(text).toContain('10 / 10');
  });

  it('renders matched and missing skills', async () => {
    service.response = of(match);

    fixture.componentRef.setInput(
      'vacancyId',
      '33333333-3333-3333-3333-333333333333',
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text =
      fixture.nativeElement.textContent as string;

    expect(text).toContain('Matched skills');
    expect(text).toContain('C#');
    expect(text).toContain('Missing skills');
    expect(text).toContain('Angular');
  });

  it('renders an error state when loading fails', async () => {
    service.response =
      throwError(() => new Error('Request failed'));

    fixture.componentRef.setInput(
      'vacancyId',
      '33333333-3333-3333-3333-333333333333',
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const alert =
      fixture.nativeElement.querySelector(
        '[role="alert"]',
      ) as HTMLElement | null;

    expect(alert).not.toBeNull();

    expect(alert?.textContent).toContain(
      'The match result could not be loaded.',
    );
  });

});

class FakeJobMatchService {
  response: Observable<MatchResult> = of({
    totalScore: 0,
    skillsScore: 0,
    experienceScore: 0,
    educationScore: 0,
    locationScore: 0,
    matchedSkills: [],
    missingSkills: [],
  });

  getMatch(
    _vacancyId: string,
  ): Observable<MatchResult> {
    return this.response;
  }
}
