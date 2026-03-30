import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

declare const google: any;

/**
 * Singleton service that ensures google.accounts.id.initialize() is called
 * exactly once per page load, preventing the "multiple initialization" console errors.
 */
@Injectable({ providedIn: 'root' })
export class GoogleAuthService {
  private initialized = false;
  private callback: ((response: { credential: string }) => void) | null = null;

  /**
   * Set up GSI with a callback. Safe to call from multiple components —
   * only the first call initialises the library; subsequent calls just
   * update the stored callback so the correct handler fires.
   */
  initialize(callback: (response: { credential: string }) => void): void {
    this.callback = callback;
    if (this.initialized) return;
    if (typeof google === 'undefined') return;

    try {
      google.accounts.id.initialize({
        client_id: environment.googleClientId,
        // Delegate to whichever callback is current at the time of the response
        callback: (response: { credential: string }) => this.callback?.(response),
        auto_select: false,
        cancel_on_tap_outside: true
      });
      this.initialized = true;
    } catch {
      // GIS library unavailable
    }
  }

  renderButton(elementId: string, options: object): void {
    if (typeof google === 'undefined' || !this.initialized) return;
    const el = document.getElementById(elementId);
    if (!el) return;
    try {
      google.accounts.id.renderButton(el, options);
    } catch { /* ignore */ }
  }
}
