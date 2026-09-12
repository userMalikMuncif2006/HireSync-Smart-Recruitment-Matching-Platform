import {
  HttpClient,
  HttpParams,
} from '@angular/common/http';
import {
  inject,
  Injectable,
} from '@angular/core';
import {
  Observable,
} from 'rxjs';

import {
  NotificationPage,
  NotificationQuery,
} from './job-seeker-notifications.models';

@Injectable({
  providedIn: 'root',
})
export class JobSeekerNotificationsService {
  private readonly http =
    inject(HttpClient);

  private readonly endpoint =
    '/api/v1/notifications';

  getOwnNotifications(
    query:
      NotificationQuery,
  ): Observable<NotificationPage> {
    const params =
      new HttpParams()
        .set(
          'page',
          query.page,
        )
        .set(
          'pageSize',
          query.pageSize,
        );

    return this.http
      .get<NotificationPage>(
        this.endpoint,
        {
          params,
        },
      );
  }

  markRead(
    notificationId: string,
  ): Observable<void> {
    return this.http
      .patch<void>(
        `${this.endpoint}/${notificationId}/read`,
        null,
      );
  }

  markAllRead():
    Observable<void> {
    return this.http
      .patch<void>(
        `${this.endpoint}/read-all`,
        null,
      );
  }
}
