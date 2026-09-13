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

  it('renders the HireSync public entry experience', () => {
    const text =
      fixture.nativeElement
        .textContent as string;

    expect(text)
      .toContain('HireSync');

    expect(text)
      .toContain('Get Started');

    expect(text)
      .toContain('Sign in');

    expect(text)
      .toContain('Sign up');
  });

  it('keeps public actions limited to authentication entry routes', () => {
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
});