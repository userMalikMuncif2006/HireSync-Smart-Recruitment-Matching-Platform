import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';

import {
  AdministratorActivationRequest,
  AdministratorActivationRequestResponse,
  AdministratorActivationVerificationResponse,
  AdministratorActivationVerifyRequest,
  EmployerOtpRequest,
  EmployerOtpVerificationResponse,
  EmployerOtpVerifyRequest,
  LoginRequest,
  LoginResponse,
  OtpRequestResponse,
  RegisterEmployerRequest,
  RegisterEmployerResponse,
  RegisterJobSeekerRequest,
  RegisterJobSeekerResponse,
} from './auth.models';
import { AuthSessionService } from './auth-session.service';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = inject(AuthSessionService);

  private readonly authBaseUrl = '/api/v1/auth';

  login(
    request: LoginRequest,
  ): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(
        `${this.authBaseUrl}/login`,
        request,
      )
      .pipe(
        tap((response) => {
          this.session.setSession(response);
        }),
      );
  }

  registerJobSeeker(
    request: RegisterJobSeekerRequest,
  ): Observable<RegisterJobSeekerResponse> {
    return this.http.post<RegisterJobSeekerResponse>(
      `${this.authBaseUrl}/register/jobseeker`,
      request,
    );
  }

  registerEmployer(
    request: RegisterEmployerRequest,
  ): Observable<RegisterEmployerResponse> {
    return this.http.post<RegisterEmployerResponse>(
      `${this.authBaseUrl}/register/employer`,
      request,
    );
  }

  requestEmployerOtp(
    request: EmployerOtpRequest,
  ): Observable<OtpRequestResponse> {
    return this.http.post<OtpRequestResponse>(
      `${this.authBaseUrl}/employer/otp/request`,
      request,
    );
  }

  verifyEmployerOtp(
    request: EmployerOtpVerifyRequest,
  ): Observable<EmployerOtpVerificationResponse> {
    return this.http.post<EmployerOtpVerificationResponse>(
      `${this.authBaseUrl}/employer/otp/verify`,
      request,
    );
  }

  requestAdministratorActivationOtp(
    request: AdministratorActivationRequest,
  ): Observable<AdministratorActivationRequestResponse> {
    return this.http.post<AdministratorActivationRequestResponse>(
      `${this.authBaseUrl}/admin/activation/otp/request`,
      request,
    );
  }

  verifyAdministratorActivationOtp(
    request: AdministratorActivationVerifyRequest,
  ): Observable<AdministratorActivationVerificationResponse> {
    return this.http.post<AdministratorActivationVerificationResponse>(
      `${this.authBaseUrl}/admin/activation/otp/verify`,
      request,
    );
  }

  logout(): void {
    this.session.clearSession();
  }
}
