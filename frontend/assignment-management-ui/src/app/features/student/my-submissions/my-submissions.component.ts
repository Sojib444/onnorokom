import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { SubmissionService } from '../../../core/services/submission.service';
import { extractError } from '../../../core/services/api-error';
import type { Submission } from '../../../core/models/submission.model';
import { EmptyComponent } from '../../../shared/components/empty.component';
import { ErrorComponent } from '../../../shared/components/error.component';
import { LoadingComponent } from '../../../shared/components/loading.component';
import { StatusClassPipe, StatusLabelPipe } from '../../../shared/pipes/status.pipe';

/** The student's own submissions with their grades and feedback, newest first. */
@Component({
  selector: 'app-my-submissions',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, LoadingComponent, EmptyComponent, ErrorComponent, StatusLabelPipe, StatusClassPipe],
  templateUrl: './my-submissions.component.html',
  styleUrl: './my-submissions.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MySubmissionsComponent implements OnInit {
  private readonly submissionsService = inject(SubmissionService);
  private readonly destroyRef = inject(DestroyRef);

  readonly items = signal<Submission[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.submissionsService
      .getMine()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (submissions) => this.items.set(submissions),
        error: (error: unknown) => this.errorMessage.set(extractError(error)),
      });
  }
}
