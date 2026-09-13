import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  inject,
  signal,
} from '@angular/core';
import {
  FormControl,
  ReactiveFormsModule,
} from '@angular/forms';
import { RouterLink } from '@angular/router';

import {
  AdminAccountStatus,
  AdminUserListItem,
  AdminUserPage,
} from './admin.models';
import { AdminService } from './admin.service';

type StatusFilter =
  | 'all'
  | 'active'
  | 'suspended';

interface PendingStatusChange {
  user: AdminUserListItem;
  status: AdminAccountStatus;
}

@Component({
  selector: 'app-admin-users-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    RouterLink,
  ],
  templateUrl: './admin-users-page.html',
  styleUrl: './admin-users-page.css',
})
export class AdminUsersPage {
  private readonly service =
    inject(AdminService);

  private readonly pageSize = 20;

  readonly searchControl =
    new FormControl(
      '',
      {
        nonNullable: true,
      },
    );

  readonly statusControl =
    new FormControl<StatusFilter>(
      'all',
      {
        nonNullable: true,
      },
    );

  readonly isLoading =
    signal(false);

  readonly loadError =
    signal<string | null>(null);

  readonly actionError =
    signal<string | null>(null);

  readonly result =
    signal<AdminUserPage | null>(null);

  readonly pendingChange =
    signal<PendingStatusChange | null>(
      null,
    );

  readonly updatingId =
    signal<string | null>(null);

  constructor() {
    this.load(1);
  }

  applyFilters(): void {
    this.load(1);
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.statusControl.setValue('all');
    this.load(1);
  }

  reload(): void {
    this.load(
      this.result()?.page ?? 1,
    );
  }

  previousPage(): void {
    const current = this.result();

    if (
      !current ||
      current.page <= 1
    ) {
      return;
    }

    this.load(current.page - 1);
  }

  nextPage(): void {
    const current = this.result();

    if (
      !current ||
      current.page >=
        this.totalPages()
    ) {
      return;
    }

    this.load(current.page + 1);
  }

  totalPages(): number {
    const current = this.result();

    if (
      !current ||
      current.totalCount === 0
    ) {
      return 1;
    }

    return Math.ceil(
      current.totalCount /
        current.pageSize,
    );
  }

  statusLabel(
    status: AdminAccountStatus,
  ): string {
    return status === 1
      ? 'Active'
      : 'Suspended';
  }

  canManage(
    user: AdminUserListItem,
  ): boolean {
    return user.role !==
      'Administrator';
  }

  requestStatusChange(
    user: AdminUserListItem,
  ): void {
    if (!this.canManage(user)) {
      return;
    }

    this.actionError.set(null);

    this.pendingChange.set({
      user,
      status:
        user.accountStatus === 1
          ? 2
          : 1,
    });
  }

  cancelStatusChange(): void {
    this.pendingChange.set(null);
  }

  confirmStatusChange(): void {
    const change =
      this.pendingChange();

    if (!change) {
      return;
    }

    this.updatingId.set(
      change.user.id,
    );

    this.actionError.set(null);

    this.service
      .updateUserStatus(
        change.user.id,
        {
          status:
            change.status,
          rowVersion:
            change.user.rowVersion,
        },
      )
      .subscribe({
        next: () => {
          this.pendingChange.set(
            null,
          );

          this.updatingId.set(
            null,
          );

          this.load(
            this.result()?.page ??
              1,
          );
        },

        error: (
          error: unknown,
        ) => {
          this.actionError.set(
            this.readError(error),
          );

          this.updatingId.set(
            null,
          );
        },
      });
  }

  private load(
    page: number,
  ): void {
    this.isLoading.set(true);
    this.loadError.set(null);
    this.actionError.set(null);
    this.pendingChange.set(null);

    this.service
      .getUsers({
        search:
          this.searchControl.value,
        status:
          this.selectedStatus(),
        page,
        pageSize:
          this.pageSize,
      })
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.isLoading.set(false);
        },

        error: (
          error: unknown,
        ) => {
          this.result.set(null);

          this.loadError.set(
            this.readError(error),
          );

          this.isLoading.set(false);
        },
      });
  }

  private selectedStatus():
    AdminAccountStatus | null {
    switch (
      this.statusControl.value
    ) {
      case 'active':
        return 1;

      case 'suspended':
        return 2;

      default:
        return null;
    }
  }

  private readError(
    error: unknown,
  ): string {
    if (
      error instanceof
        HttpErrorResponse
    ) {
      const detail =
        error.error?.detail;

      if (
        typeof detail ===
          'string' &&
        detail.trim().length > 0
      ) {
        return detail;
      }
    }

    return 'The account operation could not be completed. Please try again.';
  }
}