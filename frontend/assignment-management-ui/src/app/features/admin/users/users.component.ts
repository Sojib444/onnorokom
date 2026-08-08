import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { UserService } from '../../../core/services/user.service';
import { ClassService } from '../../../core/services/class.service';
import { extractError } from '../../../core/services/api-error';
import type { User } from '../../../core/models/user.model';
import type { Class } from '../../../core/models/class.model';
import { EmptyComponent } from '../../../shared/components/empty.component';
import { ErrorComponent } from '../../../shared/components/error.component';
import { LoadingComponent } from '../../../shared/components/loading.component';

/**
 * Admin management of users. The role is chosen at creation and never changes; the
 * class selector appears only for students. When editing, an optional "new password"
 * field resets the password after the profile update.
 */
@Component({
  selector: 'app-users',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, LoadingComponent, EmptyComponent, ErrorComponent],
  templateUrl: './users.component.html',
  styleUrl: './users.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersComponent implements OnInit {
  private readonly usersService = inject(UserService);
  private readonly classesService = inject(ClassService);
  private readonly destroyRef = inject(DestroyRef);

  readonly items = signal<User[]>([]);
  readonly classes = signal<Class[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly saving = signal(false);

  editing = signal<User | null>(null);

  /**
   * One form serves both create and edit, so its validation is swapped per mode:
   * email and role are required on create but immutable (disabled) on edit, and the
   * password is required on create but an optional ≥8-char reset on edit. The classId
   * is only meaningful for students and is nulled for other roles at submit time.
   * These are client-side convenience rules — the backend validates authoritatively.
   */
  readonly form = new FormGroup({
    fullName: new FormControl('', [Validators.required, Validators.maxLength(100)]),
    email: new FormControl('', [Validators.required, Validators.email, Validators.maxLength(254)]),
    password: new FormControl('', [Validators.minLength(8)]),
    role: new FormControl<'Admin' | 'Teacher' | 'Student'>('Student', [Validators.required]),
    classId: new FormControl<string | null>(null),
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

    this.usersService
      .getAll()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (users) => this.items.set(users),
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });

    this.classesService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (classes) => this.classes.set(classes),
    });
  }

  beginCreate(): void {
    this.editing.set(null);
    this.form.reset({ role: 'Student', classId: null });
    this.form.get('email')?.enable();
    this.form.get('password')?.setValidators([Validators.required, Validators.minLength(8)]);
    this.form.get('password')?.updateValueAndValidity();
  }

  beginEdit(user: User): void {
    this.editing.set(user);
    this.form.setValue({
      fullName: user.fullName,
      email: user.email,
      password: '',
      role: user.role as 'Admin' | 'Teacher' | 'Student',
      classId: user.classId ?? null,
    });
    // Email and role are immutable; the password field is an optional reset.
    this.form.get('email')?.disable();
    this.form.get('password')?.setValidators([Validators.minLength(8)]);
    this.form.get('password')?.updateValueAndValidity();
  }

  cancelForm(): void {
    this.editing.set(null);
    this.form.reset();
  }

  isEditing = () => this.editing() !== null;

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const editing = this.editing();
    this.saving.set(true);

    const applyPasswordReset = (): void => {
      if (!editing || !value.password) {
        return;
      }
      this.usersService
        .updatePassword(editing.id, value.password)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({ error: (error: unknown) => this.errorMessage.set(extractError(error)) });
    };

    if (editing) {
      this.usersService
        .update(editing.id, { fullName: value.fullName?.trim() ?? '', classId: value.classId ?? null })
        .pipe(
          finalize(() => this.saving.set(false)),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe({
          next: () => {
            applyPasswordReset();
            this.cancelForm();
            this.load();
          },
          error: (error: unknown) => this.errorMessage.set(extractError(error)),
        });
      return;
    }

    this.usersService
      .create({
        fullName: value.fullName?.trim() ?? '',
        email: value.email?.trim().toLowerCase() ?? '',
        password: value.password ?? '',
        role: value.role ?? 'Student',
        classId: value.role === 'Student' ? (value.classId ?? null) : null,
      })
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

  onDelete(user: User): void {
    if (!window.confirm(`Delete the account for "${user.fullName}" (${user.email})?`)) {
      return;
    }

    this.usersService.delete(user.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.load(),
      error: (error: unknown) => this.errorMessage.set(extractError(error)),
    });
  }

  fieldHasError(field: 'fullName' | 'email' | 'password'): boolean {
    const control = this.form.get(field);
    return !!control && control.invalid && control.touched;
  }
}
