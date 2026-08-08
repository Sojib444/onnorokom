import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { AssignmentService } from '../../../core/services/assignment.service';
import { TeacherAssignmentService } from '../../../core/services/teacher-assignment.service';
import { extractError } from '../../../core/services/api-error';
import type { TeacherAssignment } from '../../../core/models/teacher-assignment.model';
import type { Assignment } from '../../../core/models/assignment.model';
import { ErrorComponent } from '../../../shared/components/error.component';
import { LoadingComponent } from '../../../shared/components/loading.component';

/** Converts a server ISO deadline into the value expected by an <input type="datetime-local">. */
function toLocalInput(iso: string): string {
  const date = new Date(iso);
  const pad = (n: number): string => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

/**
 * Validates that a deadline chosen for a new assignment is still in the future.
 *
 * The validator is applied to the deadline control in both modes, but it is only
 * meaningful for creation: editing an assignment whose deadline has already passed
 * (an unavoidable real-world state once a deadline is reached) would otherwise keep
 * the form permanently invalid. The backend still rejects past deadlines for new
 * assignments independently.
 */
function futureDeadline(control: { value: string | null }): { past: true } | null {
  if (!control.value) {
    return null;
  }
  return new Date(control.value).getTime() > Date.now() ? null : { past: true };
}

/**
 * Create or edit one of the teacher's assignments. The class/subject pair is chosen
 * from the teacher's allocations (fetched from /teacher-assignments/mine); the server
 * re-checks the allocation when saving, so this is UX only.
 */
@Component({
  selector: 'app-assignment-form',
  standalone: true,
  imports: [ReactiveFormsModule, LoadingComponent, ErrorComponent],
  templateUrl: './assignment-form.component.html',
  styleUrl: './assignment-form.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssignmentFormComponent implements OnInit {
  private readonly assignmentsService = inject(AssignmentService);
  private readonly allocationsService = inject(TeacherAssignmentService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly pairs = signal<TeacherAssignment[]>([]);
  readonly loading = signal(true);
  readonly loadingAssignment = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly saving = signal(false);

  private assignmentId: string | null = null;
  protected readonly isEditing = signal(false);

  readonly form = new FormGroup({
    pairId: new FormControl('', [Validators.required]),
    title: new FormControl('', [Validators.required, Validators.maxLength(200)]),
    description: new FormControl('', [Validators.maxLength(4000)]),
    deadline: new FormControl('', [Validators.required, futureDeadline]),
    maximumMarks: new FormControl<number | null>(null, [Validators.required, Validators.min(1)]),
  });

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.errorMessage.set(null));
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.assignmentId = id;
    this.isEditing.set(id !== null);

    this.allocationsService
      .getMine()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (pairs) => {
          this.pairs.set(pairs);
          if (id) {
            this.loadAssignment(id);
          } else {
            this.loading.set(false);
          }
        },
        error: (error: unknown) => {
          this.errorMessage.set(extractError(error));
          this.loading.set(false);
        },
      });
  }

  private loadAssignment(id: string): void {
    this.loadingAssignment.set(true);
    this.assignmentsService
      .getById(id)
      .pipe(
        finalize(() => {
          this.loading.set(false);
          this.loadingAssignment.set(false);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (assignment) => this.patchForm(assignment),
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }

  private patchForm(assignment: Assignment): void {
    // The stored assignment identifies its target by classId+subjectId, while the form
    // identifies a pair by its allocation id, so the matching allocation is found by
    // comparing the two IDs rather than by pairId.
    const pair = this.pairs().find(
      (p) => p.classId === assignment.classId && p.subjectId === assignment.subjectId,
    );
    this.form.setValue({
      pairId: pair?.id ?? '',
      title: assignment.title,
      description: assignment.description,
      deadline: toLocalInput(assignment.deadline),
      maximumMarks: assignment.maximumMarks,
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.value;
    const pair = this.pairs().find((p) => p.id === value.pairId);
    if (!pair) {
      this.errorMessage.set('Choose a class and subject pair.');
      return;
    }

    const request = {
      classId: pair.classId,
      subjectId: pair.subjectId,
      title: value.title!.trim(),
      description: value.description?.trim() || '',
      deadline: new Date(value.deadline!).toISOString(),
      maximumMarks: value.maximumMarks!,
    };

    this.saving.set(true);
    const operation = this.assignmentId
      ? this.assignmentsService.update(this.assignmentId, request)
      : this.assignmentsService.create(request);

    operation
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => void this.router.navigate(['/teacher/assignments']),
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }

  onCancel(): void {
    void this.router.navigate(['/teacher/assignments']);
  }

  fieldHasError(field: 'title' | 'description' | 'deadline' | 'maximumMarks' | 'pairId'): boolean {
    const control = this.form.get(field);
    return !!control && control.invalid && control.touched;
  }
}
