import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { AssignmentService } from '../../../core/services/assignment.service';
import { extractError } from '../../../core/services/api-error';
import type { Assignment } from '../../../core/models/assignment.model';
import { EmptyComponent } from '../../../shared/components/empty.component';
import { ErrorComponent } from '../../../shared/components/error.component';
import { LoadingComponent } from '../../../shared/components/loading.component';

/**
 * The teacher's own assignments. Drafts can be edited, published or deleted; published
 * assignments link through to their submissions for grading. Drafts are grouped first
 * so unfinished work is easy to find.
 */
@Component({
  selector: 'app-teacher-assignments',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, LoadingComponent, EmptyComponent, ErrorComponent],
  templateUrl: './teacher-assignments.component.html',
  styleUrl: './teacher-assignments.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeacherAssignmentsComponent implements OnInit {
  private readonly assignmentsService = inject(AssignmentService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly items = signal<Assignment[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly busyId = signal<string | null>(null);

  readonly drafts = signal<Assignment[]>([]);
  readonly published = signal<Assignment[]>([]);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.assignmentsService
      .getAll()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (assignments) => {
          this.items.set(assignments);
          this.drafts.set(assignments.filter((a) => a.status === 'Draft'));
          this.published.set(assignments.filter((a) => a.status === 'Published'));
        },
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }

  onPublish(assignment: Assignment): void {
    if (!window.confirm(`Publish "${assignment.title}"? Students in ${assignment.className} will see it.`)) {
      return;
    }
    this.busyId.set(assignment.id);
    this.assignmentsService
      .publish(assignment.id)
      .pipe(
        finalize(() => this.busyId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.load(),
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }

  onDelete(assignment: Assignment): void {
    if (!window.confirm(`Delete the draft "${assignment.title}"? This cannot be undone.`)) {
      return;
    }
    this.busyId.set(assignment.id);
    this.assignmentsService
      .delete(assignment.id)
      .pipe(
        finalize(() => this.busyId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.load(),
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }

  onCreate(): void {
    void this.router.navigate(['/teacher/assignments/new']);
  }
}
