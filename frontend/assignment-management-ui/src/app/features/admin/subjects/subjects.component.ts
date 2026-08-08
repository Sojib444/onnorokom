import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { SubjectService } from '../../../core/services/subject.service';
import { extractError } from '../../../core/services/api-error';
import type { Subject } from '../../../core/models/subject.model';
import { EmptyComponent } from '../../../shared/components/empty.component';
import { ErrorComponent } from '../../../shared/components/error.component';
import { LoadingComponent } from '../../../shared/components/loading.component';

/**
 * Admin management of subjects. The code is the natural business key (unique) and is
 * limited to 20 characters, matching the domain invariant.
 */
@Component({
  selector: 'app-subjects',
  standalone: true,
  imports: [ReactiveFormsModule, LoadingComponent, EmptyComponent, ErrorComponent],
  templateUrl: './subjects.component.html',
  styleUrl: './subjects.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubjectsComponent implements OnInit {
  private readonly subjectsService = inject(SubjectService);
  private readonly destroyRef = inject(DestroyRef);

  readonly items = signal<Subject[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly saving = signal(false);

  editingId = signal<string | null>(null);

  readonly form = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.maxLength(100)]),
    code: new FormControl('', [Validators.required, Validators.maxLength(20)]),
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
    this.subjectsService
      .getAll()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (subjects) => this.items.set(subjects),
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }

  beginCreate(): void {
    this.editingId.set(null);
    this.form.reset();
  }

  beginEdit(item: Subject): void {
    this.editingId.set(item.id);
    this.form.setValue({ name: item.name, code: item.code });
  }

  cancelForm(): void {
    this.editingId.set(null);
    this.form.reset();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, code } = this.form.value;
    const request = { name: name!.trim(), code: code!.trim().toUpperCase() };
    const editing = this.editingId();
    this.saving.set(true);

    const operation = editing
      ? this.subjectsService.update(editing, request)
      : this.subjectsService.create(request);

    operation
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.cancelForm();
          this.load();
        },
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }

  onDelete(item: Subject): void {
    if (!window.confirm(`Delete the subject "${item.name}"? This cannot be undone.`)) {
      return;
    }

    this.subjectsService.delete(item.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.load(),
      error: (error: unknown) => this.errorMessage.set(extractError(error)),
    });
  }

  fieldHasError(field: 'name' | 'code'): boolean {
    const control = this.form.get(field);
    return !!control && control.invalid && control.touched;
  }
}
