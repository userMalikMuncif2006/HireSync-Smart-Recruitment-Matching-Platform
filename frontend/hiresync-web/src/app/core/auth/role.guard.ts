import { inject } from '@angular/core';
import {
  CanMatchFn,
  Router,
} from '@angular/router';

import { AuthSessionService } from './auth-session.service';
import { AuthRole } from './auth.models';

const roleHome: Record<AuthRole, string> = {
  JobSeeker: '/seeker',
  Employer: '/employer',
  Administrator: '/admin',
};

export const roleGuard: CanMatchFn = (route) => {
  const sessionService =
    inject(AuthSessionService);

  const router = inject(Router);

  const currentSession =
    sessionService.getValidSession();

  if (!currentSession) {
    return router.createUrlTree(['/login']);
  }

  const requiredRole =
    route.data?.['role'] as AuthRole | undefined;

  if (!requiredRole) {
    return false;
  }

  if (currentSession.role === requiredRole) {
    return true;
  }

  return router.createUrlTree([
    roleHome[currentSession.role],
  ]);
};