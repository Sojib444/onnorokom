import { Pipe, PipeTransform } from '@angular/core';

/** Formats a byte count as a human-readable size, e.g. "1.4 MB". */
@Pipe({ name: 'fileSize', standalone: true })
export class FileSizePipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value === null || value === undefined) {
      return '—';
    }

    if (value < 1024) {
      return `${value} B`;
    }

    const units = ['KB', 'MB', 'GB'];
    let size = value;
    let unit = 'B';
    for (const candidate of units) {
      size /= 1024;
      unit = candidate;
      if (size < 1024) {
        break;
      }
    }
    return `${size.toFixed(size >= 100 ? 0 : 1)} ${unit}`;
  }
}
