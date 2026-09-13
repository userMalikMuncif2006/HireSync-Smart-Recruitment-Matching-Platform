import {
  provideHttpClient,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import {
  TestBed,
} from '@angular/core/testing';

import {
  AdminService,
} from './admin.service';

describe('AdminService', () => {
  let service:
    AdminService;

  let httpTesting:
    HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AdminService,
      ],
    });

    service =
      TestBed.inject(AdminService);

    httpTesting =
      TestBed.inject(
        HttpTestingController,
      );
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('gets the Administrator dashboard summary', () => {
    service
      .getDashboard()
      .subscribe();

    const request =
      httpTesting.expectOne(
        '/api/v1/admin/dashboard',
      );

    expect(request.request.method)
      .toBe('GET');

    request.flush({
      totalUsers: 12,
      totalVacancies: 7,
      totalApplications: 18,
      calculatedAtUtc:
        '2026-09-13T05:00:00Z',
    });
  });

  it('gets paged users with search and status filters', () => {
    service
      .getUsers({
        search: ' seeker@example.com ',
        status: 2,
        page: 3,
        pageSize: 20,
      })
      .subscribe();

    const request =
      httpTesting.expectOne(
        (candidate) =>
          candidate.url ===
          '/api/v1/admin/users',
      );

    expect(request.request.method)
      .toBe('GET');

    expect(
      request.request.params.get(
        'search',
      ),
    ).toBe(
      'seeker@example.com',
    );

    expect(
      request.request.params.get(
        'status',
      ),
    ).toBe('2');

    expect(
      request.request.params.get(
        'page',
      ),
    ).toBe('3');

    expect(
      request.request.params.get(
        'pageSize',
      ),
    ).toBe('20');

    request.flush({
      items: [],
      page: 3,
      pageSize: 20,
      totalCount: 0,
    });
  });

  it('patches account status with the authoritative RowVersion', () => {
    service
      .updateUserStatus(
        '11111111-1111-1111-1111-111111111111',
        {
          status: 2,
          rowVersion: 'AQID',
        },
      )
      .subscribe();

    const request =
      httpTesting.expectOne(
        '/api/v1/admin/users/11111111-1111-1111-1111-111111111111/status',
      );

    expect(request.request.method)
      .toBe('PATCH');

    expect(request.request.body)
      .toEqual({
        status: 2,
        rowVersion: 'AQID',
      });

    request.flush({
      id:
        '11111111-1111-1111-1111-111111111111',
      displayName:
        'Test User',
      email:
        'test@example.com',
      role:
        'JobSeeker',
      accountStatus: 2,
      createdAtUtc:
        '2026-09-13T05:00:00Z',
      rowVersion:
        'BAUG',
    });
  });

  it('gets pending Employer verification records', () => {
    service
      .getPendingEmployers()
      .subscribe();

    const request =
      httpTesting.expectOne(
        '/api/v1/admin/employers/pending',
      );

    expect(request.request.method)
      .toBe('GET');

    request.flush([]);
  });

  it('approves an Employer through the existing Admin API', () => {
    service
      .approveEmployer(
        '22222222-2222-2222-2222-222222222222',
      )
      .subscribe();

    const request =
      httpTesting.expectOne(
        '/api/v1/admin/employers/22222222-2222-2222-2222-222222222222/approve',
      );

    expect(request.request.method)
      .toBe('PATCH');

    expect(request.request.body)
      .toBeNull();

    request.flush({
      userId:
        '22222222-2222-2222-2222-222222222222',
      email:
        'employer@example.com',
      displayName:
        'Example Employer',
      status: 2,
    });
  });

  it('rejects an Employer through the existing Admin API', () => {
    service
      .rejectEmployer(
        '33333333-3333-3333-3333-333333333333',
      )
      .subscribe();

    const request =
      httpTesting.expectOne(
        '/api/v1/admin/employers/33333333-3333-3333-3333-333333333333/reject',
      );

    expect(request.request.method)
      .toBe('PATCH');

    expect(request.request.body)
      .toBeNull();

    request.flush({
      userId:
        '33333333-3333-3333-3333-333333333333',
      email:
        'rejected@example.com',
      displayName:
        'Rejected Employer',
      status: 3,
    });
  });
});