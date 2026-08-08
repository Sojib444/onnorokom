import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

/**
 * Guards feature routes by role for navigation UX only. The backend is the real
 * authorization boundary; this guard merely keeps the UI coherent.
 */
export const roleGuard = (...roles: string[]): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.role() !== null && roles.includes(auth.role()!)) {
      return true;
    }

    return router.createUrlTree(['/dashboard']);
  };
};
