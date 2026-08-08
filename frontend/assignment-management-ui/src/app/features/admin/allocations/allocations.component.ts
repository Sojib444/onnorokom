import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { TeacherAssignmentService } from '../../../core/services/teacher-assignment.service';
import { UserService } from '../../../core/services/user.service';
import { ClassService } from '../../../core/services/class.service';
import { SubjectService } from '../../../core/services/subject.service';
import { extractError } from '../../../core/services/api-error';
import type { TeacherAssignment } from '../../../core/models/teacher-assignment.model';
import type { User } from '../../../core/models/user.model';
import type { Class } from '../../../core/models/class.model';
import type { Subject } from '../../../core/models/subject.model';
import { EmptyComponent } from '../../../shared/components/empty.component';
import { ErrorComponent } from '../../../shared/components/error.component';
import { LoadingComponent } from '../../../shared/components/loading.component';

/**
 * Admin management of teacher allocations: which teacher may author assignments for
 * which class/subject pair. The create form only lists users with the Teacher role.
 */
@Component({
  selector: 'app-allocations',
  standalone: true,
  imports: [ReactiveFormsModule, LoadingComponent, EmptyComponent, ErrorComponent],
  templateUrl: './allocations.component.html',
  styleUrl: './allocations.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AllocationsComponent implements OnInit {
  private readonly allocationsService = inject(TeacherAssignmentService);
  private readonly usersService = inject(UserService);
  private readonly classesService = inject(ClassService);
  private readonly subjectsService = inject(SubjectService);
  private readonly destroyRef = inject(DestroyRef);

  readonly items = signal<TeacherAssignment[]>([]);
  readonly teachers = signal<User[]>([]);
  readonly classes = signal<Class[]>([]);
  readonly subjects = signal<Subject[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly saving = signal(false);

  readonly form = new FormGroup({
    teacherId: new FormControl('', [Validators.required]),
    classId: new FormControl('', [Validators.required]),
    subjectId: new FormControl('', [Validators.required]),
  });

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.errorMessage.set(null));
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.allocationsService
      .getAll()
      .pipe(finalize(() => this.loading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => this.items.set(items),
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });

    this.usersService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (users) => this.teachers.set(users.filter((user) => user.role === 'Teacher')),
    });
    this.classesService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (classes) => this.classes.set(classes),
    });
    this.subjectsService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (subjects) => this.subjects.set(subjects),
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.form.value as { teacherId: string; classId: string; subjectId: string };
    this.saving.set(true);

    this.allocationsService
      .create(request)
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.form.reset();
          this.load();
        },
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }

  onDelete(item: TeacherAssignment): void {
    const label = `${item.teacherName} — ${item.className} / ${item.subjectName}`;
    if (!window.confirm(`Remove this allocation? (${label})`)) {
      return;
    }

    this.allocationsService.delete(item.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.load(),
      error: (error: unknown) => this.errorMessage.set(extractError(error)),
    });
  }
}
