import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

/**
 * Guards the shell routes. Without a session the user is redirected to the login page,
 * remembering where they were headed so login can return them there.
 *
 * This guard provides client-side navigation control only and is not a security
 * boundary; the backend independently validates every request.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};
