import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { AssignmentService } from '../../../core/services/assignment.service';
import { SubmissionService } from '../../../core/services/submission.service';
import { extractError } from '../../../core/services/api-error';
import { AuthService } from '../../../core/auth/auth.service';
import type { Submission, SubmissionAttachment } from '../../../core/models/submission.model';
import type { Assignment } from '../../../core/models/assignment.model';
import { EmptyComponent } from '../../../shared/components/empty.component';
import { ErrorComponent } from '../../../shared/components/error.component';
import { LoadingComponent } from '../../../shared/components/loading.component';
import { FileSizePipe } from '../../../shared/pipes/filesize.pipe';
import { StatusClassPipe, StatusLabelPipe } from '../../../shared/pipes/status.pipe';

/**
 * Submissions for one assignment. The assignment's teacher can award marks, leave
 * feedback and return work for revision; administrators see the same page read-only.
 * The backend enforces both rules regardless of what the UI shows.
 */
@Component({
  selector: 'app-submissions',
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    LoadingComponent,
    EmptyComponent,
    ErrorComponent,
    FileSizePipe,
    StatusLabelPipe,
    StatusClassPipe,
  ],
  templateUrl: './submissions.component.html',
  styleUrl: './submissions.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubmissionsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly assignmentsService = inject(AssignmentService);
  private readonly submissionsService = inject(SubmissionService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly assignment = signal<Assignment | null>(null);
  readonly items = signal<Submission[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly saving = signal(false);
  readonly gradingId = signal<string | null>(null);

  /**
   * Whether the caller is a teacher. Drives UI mode: teachers get the grade/return
   * actions while admins see the page read-only. Snapshot, not reactive, because the
   * role never changes during a route's lifetime; the backend enforces the same rule.
   */
  readonly isTeacher = this.auth.role();

  private assignmentId = '';

  /**
   * Grading form. The marks-ceiling rule (marks ≤ assignment maximum) is enforced
   * inline in onGrade rather than as a validator because the ceiling lives on the
   * loaded assignment, not on the form control; the backend remains the final check.
   */
  readonly gradeForm = new FormGroup({
    marks: new FormControl<number | null>(null, [Validators.required, Validators.min(0)]),
    feedback: new FormControl('', [Validators.maxLength(2000)]),
  });

  constructor() {
    this.gradeForm.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.errorMessage.set(null));
  }

  ngOnInit(): void {
    this.assignmentId = this.route.snapshot.paramMap.get('id') ?? '';
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.assignmentsService.getById(this.assignmentId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (assignment) => this.assignment.set(assignment),
      error: (error: unknown) => this.errorMessage.set(extractError(error)),
    });

    this.assignmentsService
      .getSubmissions(this.assignmentId)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (submissions) => this.items.set(submissions),
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }

  beginGrading(submission: Submission): void {
    this.gradingId.set(submission.id);
    this.gradeForm.setValue({ marks: submission.marks ?? null, feedback: submission.feedback ?? '' });
  }

  cancelGrading(): void {
    this.gradingId.set(null);
    this.gradeForm.reset();
  }

  onGrade(submission: Submission): void {
    if (this.gradeForm.invalid) {
      this.gradeForm.markAllAsTouched();
      return;
    }

    const assignment = this.assignment();
    const max = assignment?.maximumMarks;
    const marks = this.gradeForm.value.marks ?? null;
    if (max !== undefined && marks !== null && marks > max) {
      this.errorMessage.set(`Marks cannot exceed the maximum of ${max}.`);
      return;
    }

    const feedback = this.gradeForm.value.feedback?.trim() || null;
    this.saving.set(true);
    this.submissionsService
      .grade(submission.id, marks!, feedback)
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.cancelGrading();
          this.load();
        },
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }

  onReturnForRevision(submission: Submission): void {
    if (!window.confirm(`Return this submission to ${submission.studentName ?? 'the student'} for revision?`)) {
      return;
    }

    this.saving.set(true);
    this.submissionsService
      .returnForRevision(submission.id)
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.load(),
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }

  onDownloadAttachment(submission: Submission, attachment: SubmissionAttachment): void {
    this.submissionsService
      .downloadAttachment(submission.id, attachment.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = attachment.fileName;
          link.click();
          URL.revokeObjectURL(url);
        },
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }

  gradeFieldHasError(field: 'marks' | 'feedback'): boolean {
    const control = this.gradeForm.get(field);
    return !!control && control.invalid && control.touched;
  }
}
