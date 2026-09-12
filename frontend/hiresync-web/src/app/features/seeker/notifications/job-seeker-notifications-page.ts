import {
  DatePipe,
} from '@angular/common';
import {
  HttpErrorResponse,
} from '@angular/common/http';
import {
  Component,
  inject,
  signal,
} from '@angular/core';
import {
  Router,
} from '@angular/router';

import {
  JobSeekerNotification,
  NotificationPage,
  NotificationType,
} from './job-seeker-notifications.models';
import {
  JobSeekerNotificationsService,
} from './job-seeker-notifications.service';

@Component({
  selector:
    'app-job-seeker-notifications-page',
  standalone: true,
  imports: [
    DatePipe,
  ],
  templateUrl:
    './job-seeker-notifications-page.html',
  styleUrl:
    './job-seeker-notifications-page.css',
})
export class JobSeekerNotificationsPage {
  private readonly service =
    inject(JobSeekerNotificationsService);

  private readonly router =
    inject(Router);

  private readonly pageSize =
    20;

  readonly NotificationType =
    NotificationType;

  readonly result =
    signal<NotificationPage | null>(
      null,
    );

  readonly isLoading =
    signal(false);

  readonly errorMessage =
    signal<string | null>(
      null,
    );

  readonly actionErrorMessage =
    signal<string | null>(
      null,
    );

  readonly busyNotificationId =
    signal<string | null>(
      null,
    );

  readonly isMarkingAll =
    signal(false);

  constructor() {
    this.load(1);
  }

  retry(): void {
    this.load(
      this.result()?.page ?? 1,
    );
  }

  markRead(
    notification:
      JobSeekerNotification,
  ): void {
    if (
      notification.isRead ||
      this.busyNotificationId() !==
        null ||
      this.isMarkingAll()
    )
    {
      return;
    }

    this.actionErrorMessage.set(
      null,
    );

    this.busyNotificationId.set(
      notification.id,
    );

    this.service
      .markRead(
        notification.id,
      )
      .subscribe({
        next: () => {
          this.busyNotificationId.set(
            null,
          );

          this.load(
            this.result()?.page ?? 1,
          );
        },
        error: (
          error:
            HttpErrorResponse,
        ) => {
          this.busyNotificationId.set(
            null,
          );

          this.actionErrorMessage.set(
            this.problemMessage(
              error,
              'The notification could not be marked as read.',
            ),
          );
        },
      });
  }

  markAllRead(): void {
    const current =
      this.result();

    if (
      !current ||
      current.totalCount === 0 ||
      this.isMarkingAll() ||
      this.busyNotificationId() !==
        null
    )
    {
      return;
    }

    this.actionErrorMessage.set(
      null,
    );

    this.isMarkingAll.set(
      true,
    );

    this.service
      .markAllRead()
      .subscribe({
        next: () => {
          this.isMarkingAll.set(
            false,
          );

          this.load(
            current.page,
          );
        },
        error: (
          error:
            HttpErrorResponse,
        ) => {
          this.isMarkingAll.set(
            false,
          );

          this.actionErrorMessage.set(
            this.problemMessage(
              error,
              'Notifications could not be marked as read.',
            ),
          );
        },
      });
  }

  viewApplications(): void {
    void this.router.navigate([
      '/seeker/applications',
    ]);
  }

  previousPage(): void {
    const current =
      this.result();

    if (
      !current ||
      current.page <= 1
    )
    {
      return;
    }

    this.load(
      current.page - 1,
    );
  }

  nextPage(): void {
    const current =
      this.result();

    if (
      !current ||
      current.page >=
        this.totalPages()
    )
    {
      return;
    }

    this.load(
      current.page + 1,
    );
  }

  totalPages(): number {
    const current =
      this.result();

    if (
      !current ||
      current.totalCount === 0
    )
    {
      return 1;
    }

    return Math.ceil(
      current.totalCount /
        current.pageSize,
    );
  }

  notificationTypeLabel(
    type:
      NotificationType,
  ): string {
    switch (type)
    {
      case NotificationType
        .ApplicationStatusChanged:
        return 'Application update';

      default:
        return 'Notification';
    }
  }

  private load(
    page: number,
  ): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.service
      .getOwnNotifications({
        page,
        pageSize:
          this.pageSize,
      })
      .subscribe({
        next: (result) => {
          this.result.set(
            result,
          );

          this.isLoading.set(
            false,
          );
        },
        error: (
          error:
            HttpErrorResponse,
        ) => {
          this.errorMessage.set(
            this.problemMessage(
              error,
              'Your notifications could not be loaded.',
            ),
          );

          this.isLoading.set(
            false,
          );
        },
      });
  }

  private problemMessage(
    error:
      HttpErrorResponse,
    fallback:
      string,
  ): string {
    const body =
      error.error as
        | {
            detail?: unknown;
          }
        | null;

    if (
      body &&
      typeof body.detail ===
        'string' &&
      body.detail.trim()
        .length > 0
    )
    {
      return body.detail;
    }

    return fallback;
  }
}
