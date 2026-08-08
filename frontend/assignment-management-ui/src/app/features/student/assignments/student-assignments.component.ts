import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { AssignmentService } from '../../../core/services/assignment.service';
import { SubmissionService } from '../../../core/services/submission.service';
import { extractError } from '../../../core/services/api-error';
import type { Assignment } from '../../../core/models/assignment.model';
import type { Submission } from '../../../core/models/submission.model';
import { EmptyComponent } from '../../../shared/components/empty.component';
import { ErrorComponent } from '../../../shared/components/error.component';
import { LoadingComponent } from '../../../shared/components/loading.component';
import { StatusClassPipe, StatusLabelPipe } from '../../../shared/pipes/status.pipe';

/** An assignment combined with the student's own submission, if one exists. */
export interface StudentAssignmentRow {
  assignment: Assignment;
  submission: Submission | null;
  pastDeadline: boolean;
}

/**
 * Published assignments for the student's class. Each row shows whether the student
 * has submitted, was returned for revision, or has been graded — computed by joining
 * the student's own submissions against the visible assignments.
 */
@Component({
  selector: 'app-student-assignments',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, LoadingComponent, EmptyComponent, ErrorComponent, StatusLabelPipe, StatusClassPipe],
  templateUrl: './student-assignments.component.html',
  styleUrl: './student-assignments.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentAssignmentsComponent implements OnInit {
  private readonly assignmentsService = inject(AssignmentService);
  private readonly submissionsService = inject(SubmissionService);
  private readonly destroyRef = inject(DestroyRef);

  readonly rows = signal<StudentAssignmentRow[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);

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
          this.submissionsService.getMine().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: (submissions) => this.buildRows(assignments, submissions),
            error: (error: unknown) => this.errorMessage.set(extractError(error)),
          });
        },
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }

  private buildRows(assignments: Assignment[], submissions: Submission[]): void {
    const byAssignment = new Map<string, Submission>();
    for (const submission of submissions) {
      byAssignment.set(submission.assignmentId, submission);
    }

    const now = Date.now();
    const sorted = [...assignments].sort(
      (a, b) => new Date(a.deadline).getTime() - new Date(b.deadline).getTime(),
    );

    this.rows.set(
      sorted.map((assignment) => ({
        assignment,
        submission: byAssignment.get(assignment.id) ?? null,
        pastDeadline: new Date(assignment.deadline).getTime() < now,
      })),
    );
  }
}
