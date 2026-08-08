import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

/**
 * Functional HTTP interceptor that attaches the bearer token to every request except
 * the login call itself, and signs the user out when any other call returns 401.
 *
 * This interceptor only attaches the client-side credential; it never fabricates
 * authorization state, and it is not a security boundary — the backend validates every
 * request independently. The rethrown error is left for the caller's error handling.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const isLoginCall = request.url.includes('/api/auth/login');

  if (auth.token() && !isLoginCall) {
    request = request.clone({
      setHeaders: { Authorization: `Bearer ${auth.token()}` },
    });
  }

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !isLoginCall) {
        auth.logout();
      }
      return throwError(() => error);
    }),
  );
};
