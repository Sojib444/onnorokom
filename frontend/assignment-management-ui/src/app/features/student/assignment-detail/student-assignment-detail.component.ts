import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { AssignmentService } from '../../../core/services/assignment.service';
import { SubmissionService } from '../../../core/services/submission.service';
import { extractError } from '../../../core/services/api-error';
import type { Assignment } from '../../../core/models/assignment.model';
import type { Submission, SubmissionAttachment } from '../../../core/models/submission.model';
import { ErrorComponent } from '../../../shared/components/error.component';
import { LoadingComponent } from '../../../shared/components/loading.component';
import { FileSizePipe } from '../../../shared/pipes/filesize.pipe';
import { StatusClassPipe, StatusLabelPipe } from '../../../shared/pipes/status.pipe';

/**
 * Assignment details plus the student's submission workflow: submit an answer with an
 * optional attachment, or update it before the deadline. A submission returned for
 * revision stays editable past the deadline; a graded submission is locked.
 */
@Component({
  selector: 'app-student-assignment-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, ReactiveFormsModule, LoadingComponent, ErrorComponent, FileSizePipe, StatusLabelPipe, StatusClassPipe],
  templateUrl: './student-assignment-detail.component.html',
  styleUrl: './student-assignment-detail.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentAssignmentDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly assignmentsService = inject(AssignmentService);
  private readonly submissionsService = inject(SubmissionService);
  private readonly destroyRef = inject(DestroyRef);

  readonly assignment = signal<Assignment | null>(null);
  readonly submission = signal<Submission | null>(null);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly saving = signal(false);

  readonly canEdit = signal(false);
  readonly pastDeadline = signal(false);
  readonly returned = signal(false);
  readonly graded = signal(false);

  selectedFile: File | null = null;

  readonly form = new FormGroup({
    answer: new FormControl('', [Validators.required, Validators.maxLength(8000)]),
  });

  private assignmentId = '';

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.errorMessage.set(null));
  }

  ngOnInit(): void {
    this.assignmentId = this.route.snapshot.paramMap.get('id') ?? '';
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.assignmentsService
      .getById(this.assignmentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (assignment) => {
          this.assignment.set(assignment);
          this.pastDeadline.set(new Date(assignment.deadline).getTime() < Date.now());
          this.submissionsService
            .getMine()
            .pipe(
              finalize(() => this.loading.set(false)),
              takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
              next: (submissions) => this.applySubmission(submissions.find((s) => s.assignmentId === assignment.id) ?? null),
              error: (error: unknown) => this.errorMessage.set(extractError(error)),
            });
        },
        error: (error: unknown) => {
          this.errorMessage.set(extractError(error));
          this.loading.set(false);
        },
      });
  }

  private applySubmission(submission: Submission | null): void {
    this.submission.set(submission);
    const returned = submission?.status === 'Returned';
    const graded = submission?.status === 'Graded';
    this.returned.set(returned);
    this.graded.set(graded);

    // A submission may be edited when the deadline is still ahead, or when the
    // teacher has returned it for revision (which reopens it past the deadline).
    this.canEdit.set(!graded && (!this.pastDeadline() || returned));

    this.form.patchValue({ answer: submission?.answer ?? '' });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const answer = this.form.value.answer!.trim();
    const existing = this.submission();
    this.saving.set(true);

    const operation = existing
      ? this.submissionsService.update(existing.id, answer, this.selectedFile)
      : this.submissionsService.submit(this.assignmentId, answer, this.selectedFile);

    operation
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (submission) => {
          this.selectedFile = null;
          this.applySubmission(submission);
        },
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

  fieldHasError(): boolean {
    const control = this.form.get('answer');
    return !!control && control.invalid && control.touched;
  }
}
