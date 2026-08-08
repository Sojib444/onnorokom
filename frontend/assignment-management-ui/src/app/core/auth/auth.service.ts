import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { AuthenticatedUser, LoginRequest, LoginResponse } from '../models/auth.model';

/**
 * Holds the session: the JWT and the current user, persisted to localStorage so a
 * page refresh keeps the user signed in. Signals make the session observable by the
 * shell layout and the route guards without manual change detection.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly storageKey = 'assignment-management.session';

  private readonly tokenSignal = signal<string | null>(null);
  private readonly userSignal = signal<AuthenticatedUser | null>(null);

  /** The raw access token, read by the HTTP interceptor. */
  readonly token = this.tokenSignal.asReadonly();

  /** The current user identity, or null when signed out. */
  readonly currentUser = this.userSignal.asReadonly();

  /** Whether a token is present and therefore requests are authenticated. */
  readonly isAuthenticated = computed(() => this.tokenSignal() !== null);

  /** The caller's role, used only for navigation UX. The backend remains authoritative. */
  readonly role = computed(() => this.userSignal()?.role ?? null);

  constructor() {
    this.restoreSession();
  }

  /** Exchanges credentials for a token and stores the resulting session. */
  login(email: string, password: string): Observable<LoginResponse> {
    const body: LoginRequest = { email, password };
    return this.http.post<LoginResponse>(`${environment.apiUrl}/api/auth/login`, body).pipe(
      tap((response) => {
        this.tokenSignal.set(response.token);
        this.userSignal.set(response.user);
        localStorage.setItem(this.storageKey, JSON.stringify(response));
      }),
    );
  }

  /** Clears the session and returns the user to the login screen. */
  logout(): void {
    this.tokenSignal.set(null);
    this.userSignal.set(null);
    localStorage.removeItem(this.storageKey);
    this.router.navigate(['/login']);
  }

  /** Restores a previously persisted session, if any. */
  private restoreSession(): void {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) {
      return;
    }

    try {
      const session = JSON.parse(raw) as LoginResponse;
      this.tokenSignal.set(session.token);
      this.userSignal.set(session.user);
    } catch {
      localStorage.removeItem(this.storageKey);
    }
  }
}
