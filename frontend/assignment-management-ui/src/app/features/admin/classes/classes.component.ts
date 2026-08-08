import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { ClassService } from '../../../core/services/class.service';
import { extractError } from '../../../core/services/api-error';
import type { Class } from '../../../core/models/class.model';
import { EmptyComponent } from '../../../shared/components/empty.component';
import { ErrorComponent } from '../../../shared/components/error.component';
import { LoadingComponent } from '../../../shared/components/loading.component';

/**
 * Admin management of classes (courses). Table plus a create/edit form with required
 * and length validation; deletions are confirmed before they hit the API.
 */
@Component({
  selector: 'app-classes',
  standalone: true,
  imports: [ReactiveFormsModule, LoadingComponent, EmptyComponent, ErrorComponent],
  templateUrl: './classes.component.html',
  styleUrl: './classes.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClassesComponent implements OnInit {
  private readonly classesService = inject(ClassService);

  readonly items = signal<Class[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly saving = signal(false);

  editingId = signal<string | null>(null);

  readonly form = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.maxLength(100)]),
    description: new FormControl('', [Validators.maxLength(500)]),
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
    this.classesService
      .getAll()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (classes) => this.items.set(classes),
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }

  beginCreate(): void {
    this.editingId.set(null);
    this.form.reset();
  }

  beginEdit(item: Class): void {
    this.editingId.set(item.id);
    this.form.setValue({ name: item.name, description: item.description ?? '' });
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

    const { name, description } = this.form.value;
    const request = { name: name!.trim(), description: description?.trim() || null };
    const editing = this.editingId();
    this.saving.set(true);

    const operation = editing
      ? this.classesService.update(editing, request)
      : this.classesService.create(request);

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

  onDelete(item: Class): void {
    if (!window.confirm(`Delete the class "${item.name}"? This cannot be undone.`)) {
      return;
    }

    this.classesService.delete(item.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.load(),
      error: (error: unknown) => this.errorMessage.set(extractError(error)),
    });
  }

  private readonly destroyRef = inject(DestroyRef);

  fieldHasError(field: 'name' | 'description'): boolean {
    const control = this.form.get(field);
    return !!control && control.invalid && control.touched;
  }
}
