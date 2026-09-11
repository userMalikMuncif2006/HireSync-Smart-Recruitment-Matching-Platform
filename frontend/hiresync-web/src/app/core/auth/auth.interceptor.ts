import {
  HttpInterceptorFn,
} from '@angular/common/http';
import { inject } from '@angular/core';

import { AuthSessionService } from './auth-session.service';

export const authInterceptor: HttpInterceptorFn =
  (request, next) => {
    if (
      !request.url.startsWith('/api/') ||
      request.url.startsWith('/api/v1/auth/')
    ) {
      return next(request);
    }

    const session = inject(AuthSessionService);

    const accessToken =
      session.getValidAccessToken();

    if (!accessToken) {
      return next(request);
    }

    return next(
      request.clone({
        setHeaders: {
          Authorization: `Bearer ${accessToken}`,
        },
      }),
    );
  };