import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { DatePipe, DecimalPipe } from '@angular/common';
import { AssignmentService } from '../../../core/services/assignment.service';
import { extractError } from '../../../core/services/api-error';
import type { Assignment } from '../../../core/models/assignment.model';
import { EmptyComponent } from '../../../shared/components/empty.component';
import { ErrorComponent } from '../../../shared/components/error.component';
import { LoadingComponent } from '../../../shared/components/loading.component';
import { StatusClassPipe, StatusLabelPipe } from '../../../shared/pipes/status.pipe';

/** Read-only list of every assignment, shown to administrators. */
@Component({
  selector: 'app-admin-assignments',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, LoadingComponent, EmptyComponent, ErrorComponent, StatusLabelPipe, StatusClassPipe],
  templateUrl: './admin-assignments.component.html',
  styleUrl: './admin-assignments.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAssignmentsComponent implements OnInit {
  private readonly assignmentsService = inject(AssignmentService);
  private readonly destroyRef = inject(DestroyRef);

  readonly items = signal<Assignment[]>([]);
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
        next: (assignments) => this.items.set(assignments),
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }
}
