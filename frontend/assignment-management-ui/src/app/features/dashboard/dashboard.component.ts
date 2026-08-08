import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LowerCasePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { extractError } from '../../core/services/api-error';
import { AssignmentService } from '../../core/services/assignment.service';
import { SubmissionService } from '../../core/services/submission.service';
import { UserService } from '../../core/services/user.service';
import { ClassService } from '../../core/services/class.service';
import { SubjectService } from '../../core/services/subject.service';
import { ErrorComponent } from '../../shared/components/error.component';
import { LoadingComponent } from '../../shared/components/loading.component';

/** A dashboard statistic card. */
interface StatCard {
  label: string;
  value: number;
  link: string | null;
}

/**
 * Role-specific dashboard. Each role sees a greeting and the counts that matter to it;
 * links open the relevant management pages. The backend remains the authorization
 * boundary — this page only composes what the API already lets the caller see.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, LowerCasePipe, ErrorComponent, LoadingComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly assignmentsService = inject(AssignmentService);
  private readonly submissionsService = inject(SubmissionService);
  private readonly usersService = inject(UserService);
  private readonly classesService = inject(ClassService);
  private readonly subjectsService = inject(SubjectService);
  private readonly destroyRef = inject(DestroyRef);

  readonly currentUser = this.auth.currentUser;
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly stats = signal<StatCard[]>([]);

  readonly quickLinks = computed<{ label: string; path: string }[]>(() => {
    switch (this.auth.role()) {
      case 'Admin':
        return [
          { label: 'Manage users', path: '/admin/users' },
          { label: 'Manage classes', path: '/admin/classes' },
          { label: 'Manage subjects', path: '/admin/subjects' },
          { label: 'Teacher allocations', path: '/admin/allocations' },
          { label: 'View assignments', path: '/admin/assignments' },
        ];
      case 'Teacher':
        return [
          { label: 'My assignments', path: '/teacher/assignments' },
          { label: 'Create an assignment', path: '/teacher/assignments/new' },
        ];
      default:
        return [
          { label: 'Available assignments', path: '/student/assignments' },
          { label: 'My submissions', path: '/student/submissions' },
        ];
    }
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    // Role-specific loading: the admin path fires four independent requests that each
    // append their own card via appendStat(), so loading clears on the first completion;
    // the teacher/student paths clear it via finalize() on their last request instead.
    switch (this.auth.role()) {
      case 'Admin':
        this.loadAdminStats();
        break;
      case 'Teacher':
        this.loadTeacherStats();
        break;
      default:
        this.loadStudentStats();
        break;
    }
  }

  private loadAdminStats(): void {
    this.usersService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (users) =>
        this.setStats([
          { label: 'Users', value: users.length, link: '/admin/users' },
          { label: 'Teachers', value: users.filter((u) => u.role === 'Teacher').length, link: '/admin/users' },
          { label: 'Students', value: users.filter((u) => u.role === 'Student').length, link: '/admin/users' },
        ]),
      error: (error: unknown) => this.fail(error),
    });
    this.classesService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (classes) => this.appendStat({ label: 'Classes', value: classes.length, link: '/admin/classes' }),
      error: (error: unknown) => this.fail(error),
    });
    this.subjectsService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (subjects) => this.appendStat({ label: 'Subjects', value: subjects.length, link: '/admin/subjects' }),
      error: (error: unknown) => this.fail(error),
    });
    this.assignmentsService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (assignments) =>
        this.appendStat({ label: 'Assignments', value: assignments.length, link: '/admin/assignments' }),
      error: (error: unknown) => this.fail(error),
    });
  }

  private loadTeacherStats(): void {
    this.assignmentsService
      .getAll()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (assignments) => {
          this.stats.set([
            { label: 'Total assignments', value: assignments.length, link: '/teacher/assignments' },
            { label: 'Drafts', value: assignments.filter((a) => a.status === 'Draft').length, link: '/teacher/assignments' },
            { label: 'Published', value: assignments.filter((a) => a.status === 'Published').length, link: '/teacher/assignments' },
          ]);
        },
        error: (error: unknown) => this.fail(error),
      });
  }

  private loadStudentStats(): void {
    this.assignmentsService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (assignments) =>
        this.appendStat({ label: 'Assignments for your class', value: assignments.length, link: '/student/assignments' }),
      error: (error: unknown) => this.fail(error),
    });
    this.submissionsService
      .getMine()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (submissions) => {
          this.appendStat({
            label: 'My submissions',
            value: submissions.length,
            link: '/student/submissions',
          });
          const graded = submissions.filter((s) => s.marks !== null);
          this.appendStat({ label: 'Graded', value: graded.length, link: '/student/submissions' });
        },
        error: (error: unknown) => this.fail(error),
      });
  }

  private setStats(stats: StatCard[]): void {
    this.stats.set(stats);
    this.loading.set(false);
  }

  private appendStat(stat: StatCard): void {
    this.stats.update((stats) => [...stats, stat]);
  }

  private fail(error: unknown): void {
    this.errorMessage.set(extractError(error));
    this.loading.set(false);
  }
}
