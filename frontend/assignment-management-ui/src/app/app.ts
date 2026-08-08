import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastHostComponent } from './shared/toast/toast-host.component';

/**
 * Application root. Routing resolves the login screen or the authenticated shell,
 * so the root template only hosts the router outlet plus the toast host that
 * renders notifications from anywhere in the app.
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastHostComponent],
  template: `<router-outlet /><app-toast-host />`,
  styleUrl: './app.css',
})
export class App {}
