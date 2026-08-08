import { Pipe, PipeTransform } from '@angular/core';

const LABELS: Record<string, string> = {
  Draft: 'Draft',
  Published: 'Published',
  Submitted: 'Submitted',
  Returned: 'Returned for revision',
  Graded: 'Graded',
};

/** Renders an assignment or submission status as a friendly label. */
@Pipe({ name: 'statusLabel', standalone: true })
export class StatusLabelPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    return value ? (LABELS[value] ?? value) : '—';
  }
}

/**
 * CSS class used to tint a status chip. Maps the five business states onto four visual
 * tones: Draft and unknown values are neutral, Published/Submitted are informational,
 * Graded is success, and Returned (the out-of-band revision state) is a warning.
 */
@Pipe({ name: 'statusClass', standalone: true })
export class StatusClassPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    switch (value) {
      case 'Draft':
        return 'status-neutral';
      case 'Published':
      case 'Submitted':
        return 'status-info';
      case 'Graded':
        return 'status-success';
      case 'Returned':
        return 'status-warning';
      default:
        return 'status-neutral';
    }
  }
}
