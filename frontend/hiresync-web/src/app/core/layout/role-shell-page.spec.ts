import {
  signal,
} from '@angular/core';
import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  provideRouter,
} from '@angular/router';

import {
  AuthRole,
  LoginResponse,
} from '../auth/auth.models';
import {
  AuthSessionService,
} from '../auth/auth-session.service';
import {
  RoleShellPage,
} from './role-shell-page';

describe(
  'RoleShellPage',
  () => {
    let fixture:
      ComponentFixture<RoleShellPage>;

    let session:
      FakeSessionService;

    beforeEach(
      async () => {
        session =
          new FakeSessionService(
            'Administrator',
          );

        await TestBed
          .configureTestingModule({
            imports: [
              RoleShellPage,
            ],
            providers: [
              provideRouter([]),
              {
                provide:
                  AuthSessionService,
                useValue:
                  session,
              },
            ],
          })
          .compileComponents();

        fixture =
          TestBed.createComponent(
            RoleShellPage,
          );

        fixture.detectChanges();
      },
    );

    it('renders only the Administrator workspace navigation', () => {
      const text =
        fixture.nativeElement
          .textContent as string;

      expect(text)
        .toContain('Dashboard');

      expect(text)
        .toContain('Accounts');

      expect(text)
        .toContain(
          'Employer Verification',
        );

      expect(text)
        .not.toContain('Find Jobs');

      expect(text)
        .not.toContain('Vacancies');
    });

    it('renders the authenticated role and email', () => {
      const text =
        fixture.nativeElement
          .textContent as string;

      expect(text)
        .toContain(
          'Administrator',
        );

      expect(text)
        .toContain(
          'admin@example.com',
        );
    });
  },
);

class FakeSessionService {
  readonly role;

  readonly session;

  constructor(
    role: AuthRole,
  ) {
    this.role =
      signal<AuthRole | null>(
        role,
      );

    this.session =
      signal<LoginResponse | null>({
        accessToken:
          'token',
        expiresAtUtc:
          '2099-01-01T00:00:00Z',
        userId:
          '11111111-1111-1111-1111-111111111111',
        email:
          'admin@example.com',
        role,
      });
  }

  clearSession(): void {
    this.session.set(null);
    this.role.set(null);
  }
}