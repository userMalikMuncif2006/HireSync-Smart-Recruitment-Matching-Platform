import {
  HttpErrorResponse,
} from '@angular/common/http';
import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  By,
} from '@angular/platform-browser';
import {
  NgForm,
} from '@angular/forms';
import {
  ActivatedRoute,
  ParamMap,
  Router,
  convertToParamMap,
} from '@angular/router';
import {
  BehaviorSubject,
  Observable,
  of,
  throwError,
} from 'rxjs';

import {
  LoginRequest,
  LoginResponse,
} from '../../../core/auth/auth.models';
import {
  AuthService,
} from '../../../core/auth/auth.service';
import {
  LoginPage,
} from './login-page';

describe('LoginPage', () => {
  const employerResponse:
    LoginResponse = {
      accessToken:
        'employer-token',
      expiresAtUtc:
        '2099-01-01T00:00:00Z',
      userId:
        '11111111-1111-1111-1111-111111111111',
      email:
        'employer@example.com',
      role:
        'Employer',
    };

  let auth:
    FakeAuthService;

  let router:
    FakeRouter;

  let route:
    FakeActivatedRoute;

  let fixture:
    ComponentFixture<LoginPage>;

  let component:
    LoginPage;

  beforeEach(async () => {
    auth =
      new FakeAuthService();

    router =
      new FakeRouter();

    route =
      new FakeActivatedRoute();

    await TestBed
      .configureTestingModule({
        imports: [
          LoginPage,
        ],
        providers: [
          {
            provide:
              AuthService,
            useValue:
              auth,
          },
          {
            provide:
              Router,
            useValue:
              router,
          },
          {
            provide:
              ActivatedRoute,
            useValue:
              route,
          },
        ],
      })
      .compileComponents();

    fixture =
      TestBed.createComponent(
        LoginPage,
      );

    component =
      fixture.componentInstance;

    fixture.detectChanges();

    await fixture.whenStable();
  });

  function loginForm(): NgForm {
    return fixture
      .debugElement
      .query(
        By.directive(NgForm),
      )
      .injector
      .get(NgForm);
  }

  it('uses a template-driven Angular form', () => {
    const form =
      loginForm();

    expect(form)
      .toBeTruthy();

    expect(
      fixture.nativeElement
        .querySelector(
          '[formgroup]',
        ),
    ).toBeNull();

    expect(
      fixture.nativeElement
        .querySelector(
          'input[name="email"]',
        ),
    ).not.toBeNull();

    expect(
      fixture.nativeElement
        .querySelector(
          'input[name="password"]',
        ),
    ).not.toBeNull();
  });

  it('submits trimmed email and preserves the password value', () => {
    component.credentials.email =
      '  employer@example.com  ';

    component.credentials.password =
      ' Password123! ';

    fixture.detectChanges();

    component.submit(
      loginForm(),
    );

    expect(
      auth.loginRequests,
    ).toEqual([
      {
        email:
          'employer@example.com',
        password:
          ' Password123! ',
      },
    ]);

    expect(
      router.destinations,
    ).toEqual([
      '/employer',
    ]);
  });

  it('does not submit an invalid form', () => {
    component.credentials.email =
      '';

    component.credentials.password =
      '';

    fixture.detectChanges();

    const form =
      loginForm();

    component.submit(form);

    expect(
      auth.loginRequests,
    ).toHaveLength(0);

    expect(
      form.controls['email']
        .touched,
    ).toBe(true);

    expect(
      form.controls['password']
        .touched,
    ).toBe(true);
  });

  it('shows an invalid credentials message for a 401 response', () => {
    auth.loginResult =
      throwError(
        () =>
          new HttpErrorResponse({
            status: 401,
          }),
      );

    component.credentials.email =
      'employer@example.com';

    component.credentials.password =
      'WrongPassword';

    fixture.detectChanges();

    component.submit(
      loginForm(),
    );

    fixture.detectChanges();

    expect(
      component.errorMessage(),
    ).toBe(
      'The email or password is incorrect.',
    );

    expect(
      fixture.nativeElement
        .textContent,
    ).toContain(
      'The email or password is incorrect.',
    );
  });

  it('redirects an unverified Job Seeker to email verification', () => {
    auth.loginResult =
      throwError(
        () =>
          new HttpErrorResponse({
            status: 403,
            error: {
              title:
                'Job Seeker email verification required',
            },
          }),
      );

    component.credentials.email =
      '  seeker@example.com  ';

    component.credentials.password =
      'Password123!';

    fixture.detectChanges();

    component.submit(
      loginForm(),
    );

    expect(
      router.navigations,
    ).toEqual([
      {
        commands: [
          '/verify-jobseeker-email',
        ],
        queryParams: {
          email: 'seeker@example.com',
        },
      },
    ]);

    expect(
      component.errorMessage(),
    ).toBeNull();
  });

  it('does not redirect another 403 response to Job Seeker verification', () => {
    auth.loginResult =
      throwError(
        () =>
          new HttpErrorResponse({
            status: 403,
            error: {
              title:
                'Account suspended',
            },
          }),
      );

    component.credentials.email =
      'seeker@example.com';

    component.credentials.password =
      'Password123!';

    fixture.detectChanges();

    component.submit(
      loginForm(),
    );

    expect(
      router.navigations,
    ).toHaveLength(0);

    expect(
      component.errorMessage(),
    ).toBe(
      'Sign in is not available for this account.',
    );
  });
  it('redirects Administrator login to the admin role area', () => {
    auth.loginResult =
      of({
        ...employerResponse,
        role:
          'Administrator',
      });

    component.credentials.email =
      'admin@example.com';

    component.credentials.password =
      'Password123!';

    fixture.detectChanges();

    component.submit(
      loginForm(),
    );

    expect(
      router.destinations,
    ).toEqual([
      '/admin',
    ]);
  });

  it('shows Job Seeker registration success', () => {
    route.setQueryParams({
      registered:
        'jobseeker',
    });

    fixture.detectChanges();

    expect(
      component.successMessage(),
    ).toBe(
      'Job Seeker account created successfully. You can now sign in.',
    );

    expect(
      fixture.nativeElement
        .textContent,
    ).toContain(
      'Job Seeker account created successfully.',
    );
  });

  it('shows Job Seeker email verification success', () => {
    route.setQueryParams({
      verified:
        'jobseeker',
    });

    fixture.detectChanges();

    expect(
      component.successMessage(),
    ).toBe(
      'Job Seeker email verified successfully. You can now sign in.',
    );

    expect(
      fixture.nativeElement
        .textContent,
    ).toContain(
      'Job Seeker email verified successfully.',
    );
  });
  it('shows Employer verification success while preserving approval lifecycle', () => {
    route.setQueryParams({
      verified:
        'employer',
    });

    fixture.detectChanges();

    expect(
      component.successMessage(),
    ).toContain(
      'Administrator approval',
    );

    expect(
      fixture.nativeElement
        .textContent,
    ).toContain(
      'Employer email verified successfully.',
    );
  });

  it('shows Administrator first-activation success', () => {
    route.setQueryParams({
      activated:
        'administrator',
    });

    fixture.detectChanges();

    expect(
      component.successMessage(),
    ).toBe(
      'Administrator activation completed successfully. You can now sign in.',
    );

    expect(
      fixture.nativeElement
        .textContent,
    ).toContain(
      'Administrator activation completed successfully.',
    );
  });

  it('does not show a success message for unrelated query parameters', () => {
    route.setQueryParams({
      source:
        'unknown',
    });

    fixture.detectChanges();

    expect(
      component.successMessage(),
    ).toBeNull();
  });

  class FakeAuthService {
    loginResult:
      Observable<LoginResponse> =
        of(
          employerResponse,
        );

    readonly loginRequests:
      LoginRequest[] = [];

    login(
      request: LoginRequest,
    ): Observable<LoginResponse> {
      this.loginRequests.push(
        request,
      );

      return this.loginResult;
    }
  }

  class FakeRouter {
    readonly navigations:
      Array<{
        commands: string[];
        queryParams:
          Record<string, string>;
      }> = [];
    readonly destinations:
      string[] = [];

    navigate(
      commands: string[],
      extras: {
        queryParams:
          Record<string, string>;
      },
    ): Promise<boolean> {
      this.navigations.push({
        commands,
        queryParams:
          extras.queryParams,
      });

      return Promise.resolve(
        true,
      );
    }
    navigateByUrl(
      url: string,
    ): Promise<boolean> {
      this.destinations.push(
        url,
      );

      return Promise.resolve(
        true,
      );
    }
  }

  class FakeActivatedRoute {
    private readonly params =
      new BehaviorSubject<ParamMap>(
        convertToParamMap({}),
      );

    readonly queryParamMap =
      this.params.asObservable();

    setQueryParams(
      params:
        Record<string, string>,
    ): void {
      this.params.next(
        convertToParamMap(
          params,
        ),
      );
    }
  }
});