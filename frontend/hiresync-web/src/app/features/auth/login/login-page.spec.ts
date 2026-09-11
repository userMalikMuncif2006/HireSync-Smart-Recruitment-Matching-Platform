import { HttpErrorResponse } from '@angular/common/http';
import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  Observable,
  of,
  throwError,
} from 'rxjs';

import {
  LoginRequest,
  LoginResponse,
} from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { LoginPage } from './login-page';
import { Router } from '@angular/router';

describe('LoginPage', () => {
  const employerResponse: LoginResponse = {
    accessToken: 'employer-token',
    expiresAtUtc: '2099-01-01T00:00:00Z',
    userId: '11111111-1111-1111-1111-111111111111',
    email: 'employer@example.com',
    role: 'Employer',
  };

  let auth: FakeAuthService;
  let router: FakeRouter;
  let fixture: ComponentFixture<LoginPage>;
  let component: LoginPage;

  beforeEach(async () => {
    auth = new FakeAuthService();
    router = new FakeRouter();

    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        {
          provide: AuthService,
          useValue: auth,
        },
        {
          provide: Router,
          useValue: router,
        },
      ],
    }).compileComponents();

    fixture =
      TestBed.createComponent(LoginPage);

    component =
      fixture.componentInstance;

    fixture.detectChanges();
  });

  it('submits trimmed email and preserves the password value', () => {
    component.form.setValue({
      email: '  employer@example.com  ',
      password: ' Password123! ',
    });

    component.submit();

    expect(auth.loginRequests).toEqual([
      {
        email: 'employer@example.com',
        password: ' Password123! ',
      },
    ]);

    expect(router.destinations).toEqual([
      '/employer',
    ]);
  });

  it('does not submit an invalid form', () => {
    component.form.setValue({
      email: '',
      password: '',
    });

    component.submit();

    expect(auth.loginRequests).toHaveLength(0);
    expect(component.form.controls.email.touched)
      .toBe(true);
    expect(component.form.controls.password.touched)
      .toBe(true);
  });

  it('shows an invalid credentials message for a 401 response', () => {
    auth.loginResult = throwError(
      () =>
        new HttpErrorResponse({
          status: 401,
        }),
    );

    component.form.setValue({
      email: 'employer@example.com',
      password: 'WrongPassword',
    });

    component.submit();
    fixture.detectChanges();

    expect(component.errorMessage()).toBe(
      'The email or password is incorrect.',
    );

    expect(
      fixture.nativeElement.textContent,
    ).toContain(
      'The email or password is incorrect.',
    );
  });

  it('redirects Administrator login to the admin role area', () => {
    auth.loginResult = of({
      ...employerResponse,
      role: 'Administrator',
    });

    component.form.setValue({
      email: 'admin@example.com',
      password: 'Password123!',
    });

    component.submit();

    expect(router.destinations).toEqual([
      '/admin',
    ]);
  });

  class FakeAuthService {
    loginResult: Observable<LoginResponse> =
      of(employerResponse);

    readonly loginRequests: LoginRequest[] = [];

    login(
      request: LoginRequest,
    ): Observable<LoginResponse> {
      this.loginRequests.push(request);
      return this.loginResult;
    }
  }

  class FakeRouter {
    readonly destinations: string[] = [];

    navigateByUrl(url: string): Promise<boolean> {
      this.destinations.push(url);
      return Promise.resolve(true);
    }
  }
});