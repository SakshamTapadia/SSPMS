import { Component, HostListener } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  standalone: false,
  styleUrl: './app.component.scss'
})
export class AppComponent {
  constructor(private auth: AuthService, private router: Router) {}

  // Fires when the browser restores a page from the Back/Forward Cache (bfcache).
  // Without this, the in-memory BehaviorSubject state could be stale after logout,
  // letting the user navigate back to a "authenticated" page snapshot.
  @HostListener('window:pageshow', ['$event'])
  onPageShow(event: PageTransitionEvent): void {
    if (!event.persisted) return; // Normal load — nothing to do
    const url = this.router.url;
    const onAuthPage = url.startsWith('/login') || url.startsWith('/register') || url.startsWith('/forgot');
    if (!this.auth.currentUser && !onAuthPage) {
      // Session gone but bfcache restored an authenticated page — force back to login
      this.router.navigate(['/login'], { replaceUrl: true });
    } else if (this.auth.currentUser && onAuthPage) {
      // Session present but bfcache restored the login page — skip it
      const u = this.auth.currentUser;
      const dest = u.role === 'Admin' ? '/admin/dashboard' : u.role === 'Trainer' ? '/trainer/dashboard' : '/employee/dashboard';
      this.router.navigateByUrl(dest, { replaceUrl: true });
    }
  }
}
