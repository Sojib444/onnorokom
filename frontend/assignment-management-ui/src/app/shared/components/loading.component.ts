import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Full-width loading row shown while a resource is being fetched. */
@Component({
  selector: 'app-loading',
  standalone: true,
  imports: [],
  template: ` <div class="state-row"><span class="spinner" aria-hidden="true"></span><span>{{ message() }}</span></div> `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoadingComponent {
  readonly message = input<string>('Loading…');
}
