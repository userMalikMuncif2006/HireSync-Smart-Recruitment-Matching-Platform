import { provideRouter } from '@angular/router';
import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  Observable,
  of,
} from 'rxjs';

import {
  AdminUserListItem,
  AdminUserPage,
  AdminUserQuery,
  UpdateAdminAccountStatusPayload,
} from './admin.models';
import {
  AdminUsersPage,
} from './admin-users-page';
import {
  AdminService,
} from './admin.service';

describe('AdminUsersPage', () => {
  let fixture:
    ComponentFixture<AdminUsersPage>;

  let service:
    FakeAdminService;

  async function createComponent():
    Promise<void> {
    service =
      new FakeAdminService();

    await TestBed
      .configureTestingModule({
        imports: [
          AdminUsersPage,
        ],
        providers: [
            provideRouter([]),
          {
            provide:
              AdminService,
            useValue:
              service,
          },
        ],
      })
      .compileComponents();

    fixture =
      TestBed.createComponent(
        AdminUsersPage,
      );

    fixture.detectChanges();

    await fixture.whenStable();

    fixture.detectChanges();
  }

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders account data from the server', async () => {
    await createComponent();

    const text =
      fixture.nativeElement
        .textContent as string;

    expect(text)
      .toContain('Example Seeker');

    expect(text)
      .toContain('seeker@example.com');

    expect(text)
      .toContain('Active');
  });

  it('applies search and status filters', async () => {
    await createComponent();

    fixture.componentInstance
      .searchControl
      .setValue(
        'seeker@example.com',
      );

    fixture.componentInstance
      .statusControl
      .setValue('suspended');

    fixture.componentInstance
      .applyFilters();

    expect(
      service.queries.at(-1),
    ).toEqual({
      search:
        'seeker@example.com',
      status: 2,
      page: 1,
      pageSize: 20,
    });
  });

  it('sends RowVersion during suspension and reloads authoritative state', async () => {
    await createComponent();

    const user =
      service.page.items[0];

    fixture.componentInstance
      .requestStatusChange(user);

    fixture.componentInstance
      .confirmStatusChange();

    expect(
      service.updates,
    ).toEqual([
      {
        userId: user.id,
        payload: {
          status: 2,
          rowVersion:
            user.rowVersion,
        },
      },
    ]);

    expect(
      service.queries.length,
    ).toBe(2);
  });

  it('does not expose account mutation for Administrator rows', async () => {
    await createComponent();

    expect(
      fixture.componentInstance
        .canManage(
          service.page.items[1],
        ),
    ).toBe(false);
  });
});

class FakeAdminService {
  readonly page:
    AdminUserPage = {
      items: [
        {
          id:
            '11111111-1111-1111-1111-111111111111',
          displayName:
            'Example Seeker',
          email:
            'seeker@example.com',
          role:
            'JobSeeker',
          accountStatus: 1,
          createdAtUtc:
            '2026-09-13T05:00:00Z',
          rowVersion:
            'AQID',
        },
        {
          id:
            '22222222-2222-2222-2222-222222222222',
          displayName:
            'System Administrator',
          email:
            'admin@example.com',
          role:
            'Administrator',
          accountStatus: 1,
          createdAtUtc:
            '2026-09-13T05:00:00Z',
          rowVersion:
            'BAUG',
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 2,
    };

  readonly queries:
    AdminUserQuery[] = [];

  readonly updates:
    {
      userId: string;
      payload:
        UpdateAdminAccountStatusPayload;
    }[] = [];

  getUsers(
    query: AdminUserQuery,
  ): Observable<AdminUserPage> {
    this.queries.push(query);

    return of(this.page);
  }

  updateUserStatus(
    userId: string,
    payload:
      UpdateAdminAccountStatusPayload,
  ): Observable<AdminUserListItem> {
    this.updates.push({
      userId,
      payload,
    });

    return of({
      ...this.page.items[0],
      accountStatus:
        payload.status,
      rowVersion:
        'CQgH',
    });
  }
}