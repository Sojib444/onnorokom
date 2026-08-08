import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';

/** A navigation entry shown only to a specific role. */
interface NavEntry {
  label: string;
  path: string;
  roles: string[];
}

/** Navigation entries visible to every signed-in role. */
const COMMON_NAV: NavEntry[] = [
  { label: 'Dashboard', path: '/dashboard', roles: ['Admin', 'Teacher', 'Student'] },
];

/** Admin navigation entries. */
const ADMIN_NAV: NavEntry[] = [
  { label: 'Users', path: '/admin/users', roles: ['Admin'] },
  { label: 'Classes', path: '/admin/classes', roles: ['Admin'] },
  { label: 'Subjects', path: '/admin/subjects', roles: ['Admin'] },
  { label: 'Teacher assignments', path: '/admin/allocations', roles: ['Admin'] },
  { label: 'Assignments', path: '/admin/assignments', roles: ['Admin'] },
];

/** Teacher navigation entries. */
const TEACHER_NAV: NavEntry[] = [
  { label: 'My assignments', path: '/teacher/assignments', roles: ['Teacher'] },
];

/** Student navigation entries. */
const STUDENT_NAV: NavEntry[] = [
  { label: 'Assignments', path: '/student/assignments', roles: ['Student'] },
  { label: 'My submissions', path: '/student/submissions', roles: ['Student'] },
];

/**
 * Application shell: top bar with the brand, role-aware navigation, the signed-in user
 * and a logout action. Feature pages render in the outlet below.
 */
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent {
  private readonly auth = inject(AuthService);

  readonly currentUser = this.auth.currentUser;
  readonly role = this.auth.role;

  /** Whether the mobile navigation drawer is open. Desktop layout ignores this. */
  readonly menuOpen = signal(false);

  /**
   * Navigation entries for the signed-in role. Every role array is merged and filtered
   * by the current role; the common Dashboard entry lives only in COMMON_NAV so it is
   * never rendered more than once. Role awareness here is navigation UX only.
   */
  readonly navEntries = computed(() => {
    const role = this.auth.role();
    const all = [...COMMON_NAV, ...ADMIN_NAV, ...TEACHER_NAV, ...STUDENT_NAV];
    return all.filter((entry) => entry.roles.includes(role ?? ''));
  });

  onLogout(): void {
    this.auth.logout();
  }

  /** Toggles the mobile navigation drawer open/closed. */
  toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  /** Closes the mobile navigation drawer, e.g. after a link is followed. */
  closeMenu(): void {
    this.menuOpen.set(false);
  }
}
