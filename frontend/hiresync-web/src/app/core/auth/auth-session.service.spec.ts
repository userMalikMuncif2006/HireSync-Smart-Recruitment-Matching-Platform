import { AuthSessionService } from './auth-session.service';
import { LoginResponse } from './auth.models';

describe('AuthSessionService', () => {
  const validSession: LoginResponse = {
    accessToken: 'test-token',
    expiresAtUtc: '2099-01-01T00:00:00Z',
    userId: '11111111-1111-1111-1111-111111111111',
    email: 'employer@example.com',
    role: 'Employer',
  };

  beforeEach(() => {
    sessionStorage.clear();
  });

  it('stores authenticated session data in sessionStorage', () => {
    const service = new AuthSessionService();

    service.setSession(validSession);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.accessToken()).toBe('test-token');
    expect(service.role()).toBe('Employer');
    expect(service.hasRole('Employer')).toBe(true);

    const stored =
      sessionStorage.getItem('hiresync.auth.session');

    expect(stored).not.toBeNull();
  });

  it('restores a valid browser session', () => {
    sessionStorage.setItem(
      'hiresync.auth.session',
      JSON.stringify(validSession),
    );

    const service = new AuthSessionService();

    expect(service.session()).toEqual(validSession);
    expect(service.isAuthenticated()).toBe(true);
  });

  it('rejects an expired stored session', () => {
    sessionStorage.setItem(
      'hiresync.auth.session',
      JSON.stringify({
        ...validSession,
        expiresAtUtc: '2000-01-01T00:00:00Z',
      }),
    );

    const service = new AuthSessionService();

    expect(service.session()).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
    expect(
      sessionStorage.getItem('hiresync.auth.session'),
    ).toBeNull();
  });

  it('clears the current session', () => {
    const service = new AuthSessionService();

    service.setSession(validSession);
    service.clearSession();

    expect(service.session()).toBeNull();
    expect(service.accessToken()).toBeNull();
    expect(service.role()).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
  });
});