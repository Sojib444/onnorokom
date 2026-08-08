import { Injectable, signal } from '@angular/core';

/** A single transient notification shown by the toast host. */
export interface Toast {
  id: number;
  message: string;
  type: 'error' | 'success' | 'info';
}

/**
 * Application-wide toast notifications. Any component can call
 * {@link error} or {@link success}; the {@link ToastHostComponent}
 * mounted in the root renders them and auto-dismisses each after a delay.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly toastsSignal = signal<Toast[]>([]);
  readonly toasts = this.toastsSignal.asReadonly();

  private nextId = 1;

  show(message: string, type: Toast['type'] = 'info', duration = 4000): void {
    const toast: Toast = { id: this.nextId++, message, type };
    this.toastsSignal.update((current) => [...current, toast]);
    setTimeout(() => this.dismiss(toast.id), duration);
  }

  error(message: string, duration = 4000): void {
    this.show(message, 'error', duration);
  }

  success(message: string, duration = 4000): void {
    this.show(message, 'success', duration);
  }

  dismiss(id: number): void {
    this.toastsSignal.update((current) => current.filter((t) => t.id !== id));
  }
}
