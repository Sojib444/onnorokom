import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/** Error state shown when a request fails, with an optional retry action. */
@Component({
  selector: 'app-error',
  standalone: true,
  imports: [],
  template: `
    <div class="state-row state-error">
      <span>{{ message() }}</span>
      @if (retryable()) {
        <button type="button" class="btn btn-small" (click)="retry.emit()">Try again</button>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorComponent {
  readonly message = input<string>('Something went wrong.');
  readonly retryable = input(false);
  readonly retry = output<void>();
}
