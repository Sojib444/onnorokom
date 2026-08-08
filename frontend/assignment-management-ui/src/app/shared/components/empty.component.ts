import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Empty state placeholder shown when a list has nothing to display. */
@Component({
  selector: 'app-empty',
  standalone: true,
  imports: [],
  template: ` <div class="state-row state-empty"><span>{{ message() }}</span></div> `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyComponent {
  readonly message = input<string>('Nothing here yet.');
}
