import {
  HttpClient,
  HttpParams,
} from '@angular/common/http';
import {
  inject,
  Injectable,
} from '@angular/core';
import { Observable } from 'rxjs';

import {
  AdminDashboardSummary,
  AdminUserListItem,
  AdminUserPage,
  AdminUserQuery,
  EmployerVerificationSummary,
  UpdateAdminAccountStatusPayload,
} from './admin.models';

@Injectable({
  providedIn: 'root',
})
export class AdminService {
  private readonly http =
    inject(HttpClient);

  private readonly adminBaseUrl =
    '/api/v1/admin';

  getDashboard():
    Observable<AdminDashboardSummary> {
    return this.http.get<AdminDashboardSummary>(
      `${this.adminBaseUrl}/dashboard`,
    );
  }

  getUsers(
    query: AdminUserQuery,
  ): Observable<AdminUserPage> {
    let params =
      new HttpParams()
        .set('page', query.page)
        .set('pageSize', query.pageSize);

    const search =
      query.search?.trim();

    if (search) {
      params =
        params.set(
          'search',
          search,
        );
    }

    if (
      query.status !== null &&
      query.status !== undefined
    ) {
      params =
        params.set(
          'status',
          query.status,
        );
    }

    return this.http.get<AdminUserPage>(
      `${this.adminBaseUrl}/users`,
      {
        params,
      },
    );
  }

  updateUserStatus(
    userId: string,
    payload: UpdateAdminAccountStatusPayload,
  ): Observable<AdminUserListItem> {
    return this.http.patch<AdminUserListItem>(
      `${this.adminBaseUrl}/users/${encodeURIComponent(userId)}/status`,
      payload,
    );
  }

  getPendingEmployers():
    Observable<EmployerVerificationSummary[]> {
    return this.http.get<
      EmployerVerificationSummary[]
    >(
      `${this.adminBaseUrl}/employers/pending`,
    );
  }

  approveEmployer(
    employerUserId: string,
  ): Observable<EmployerVerificationSummary> {
    return this.http.patch<
      EmployerVerificationSummary
    >(
      `${this.adminBaseUrl}/employers/${encodeURIComponent(employerUserId)}/approve`,
      null,
    );
  }

  rejectEmployer(
    employerUserId: string,
  ): Observable<EmployerVerificationSummary> {
    return this.http.patch<
      EmployerVerificationSummary
    >(
      `${this.adminBaseUrl}/employers/${encodeURIComponent(employerUserId)}/reject`,
      null,
    );
  }
}