import { computed, Injectable, signal } from '@angular/core';

import {
  AuthRole,
  LoginResponse,
} from './auth.models';

@Injectable({
  providedIn: 'root',
})
export class AuthSessionService {
  private readonly storageKey = 'hiresync.auth.session';

  private readonly sessionState =
    signal<LoginResponse | null>(
      this.readStoredSession(),
    );

  readonly session = this.sessionState.asReadonly();

  readonly isAuthenticated = computed(
    () => this.sessionState() !== null,
  );

  readonly role = computed(
    () => this.sessionState()?.role ?? null,
  );

  readonly accessToken = computed(
    () => this.sessionState()?.accessToken ?? null,
  );

  setSession(response: LoginResponse): void {
    if (this.isExpired(response.expiresAtUtc)) {
      this.clearSession();
      return;
    }

    sessionStorage.setItem(
      this.storageKey,
      JSON.stringify(response),
    );

    this.sessionState.set(response);
  }

  clearSession(): void {
    sessionStorage.removeItem(this.storageKey);
    this.sessionState.set(null);
  }

  hasRole(role: AuthRole): boolean {
    return this.sessionState()?.role === role;
  }

  private readStoredSession(): LoginResponse | null {
    const raw = sessionStorage.getItem(this.storageKey);

    if (!raw) {
      return null;
    }

    try {
      const parsed = JSON.parse(raw) as LoginResponse;

      if (
        !parsed.accessToken ||
        !parsed.expiresAtUtc ||
        !parsed.userId ||
        !parsed.email ||
        !this.isKnownRole(parsed.role) ||
        this.isExpired(parsed.expiresAtUtc)
      ) {
        sessionStorage.removeItem(this.storageKey);
        return null;
      }

      return parsed;
    } catch {
      sessionStorage.removeItem(this.storageKey);
      return null;
    }
  }

  private isKnownRole(role: string): role is AuthRole {
    return (
      role === 'JobSeeker' ||
      role === 'Employer' ||
      role === 'Administrator'
    );
  }

  private isExpired(expiresAtUtc: string): boolean {
    const expiresAt = Date.parse(expiresAtUtc);

    return (
      Number.isNaN(expiresAt) ||
      expiresAt <= Date.now()
    );
  }
}