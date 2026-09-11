import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';

import {
  LoginRequest,
  LoginResponse,
} from './auth.models';
import { AuthSessionService } from './auth-session.service';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = inject(AuthSessionService);

  private readonly loginEndpoint =
    '/api/v1/auth/login';

  login(
    request: LoginRequest,
  ): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(
        this.loginEndpoint,
        request,
      )
      .pipe(
        tap((response) => {
          this.session.setSession(response);
        }),
      );
  }

  logout(): void {
    this.session.clearSession();
  }
}