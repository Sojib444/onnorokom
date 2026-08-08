import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';

/**
 * Root dependency-injection wiring for the standalone application.
 *
 * The auth interceptor is registered here so every HttpClient call goes through it
 * (token attachment, 401 sign-out). Component input binding passes route params/query
 * params to routed components as inputs, which keeps the detail pages free of explicit
 * ActivatedRoute plumbing. This is the one place the whole wiring contract is visible.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideRouter(routes, withComponentInputBinding()),
  ],
};
