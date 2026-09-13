import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  provideRouter,
} from '@angular/router';

import {
  LandingPage,
} from './landing-page';

describe('LandingPage', () => {
  let fixture:
    ComponentFixture<LandingPage>;

  beforeEach(async () => {
    await TestBed
      .configureTestingModule({
        imports: [
          LandingPage,
        ],
        providers: [
          provideRouter([]),
        ],
      })
      .compileComponents();

    fixture =
      TestBed.createComponent(
        LandingPage,
      );

    fixture.detectChanges();
  });

  it('renders the expanded HireSync public landing experience', () => {
    const text =
      fixture.nativeElement
        .textContent as string;

    expect(text)
      .toContain('HireSync');

    expect(text)
      .toContain('Hire smarter. Build stronger teams.');

    expect(text)
      .toContain('Find opportunities that fit you.');

    expect(text)
      .toContain('Why HireSync is unique');

    expect(text)
      .toContain('About HireSync');

    expect(text)
      .toContain('Get Started');

    expect(text)
      .toContain('Sign in');

    expect(text)
      .toContain('Sign up');
  });

  it('keeps route-changing public actions limited to authentication entry routes', () => {
    const links =
      Array.from(
        fixture.nativeElement
          .querySelectorAll('a'),
      ) as HTMLAnchorElement[];

    const hrefs =
      links.map(
        (link) =>
          link.getAttribute('href'),
      );

    expect(hrefs)
      .toEqual(
        expect.arrayContaining([
          '/login',
          '/register',
        ]),
      );

    expect(
      new Set(hrefs),
    ).toEqual(
      new Set([
        '/login',
        '/register',
      ]),
    );
  });

  it('uses the local professional landing assets', () => {
    const imageSources =
      Array.from(
        fixture.nativeElement
          .querySelectorAll('img'),
      )
        .map(
          (image) =>
            (image as HTMLImageElement)
              .getAttribute('src'),
        );

    expect(imageSources)
      .toEqual([
        '/images/landing/hero-team.webp',
        '/images/landing/employer-workspace.webp',
        '/images/landing/jobseeker-workspace.webp',
        '/images/landing/hiresync-office.webp',
      ]);
  });
});