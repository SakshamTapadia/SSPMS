import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({ selector: 'app-forgot-password', standalone: false, templateUrl: './forgot-password.component.html', styleUrl: './forgot-password.component.scss' })
export class ForgotPasswordComponent {
  email = '';
  loading = false;
  constructor(private auth: AuthService, private router: Router, private snack: MatSnackBar) {}
  sent = false;

  send(): void {
    if (!this.email.trim()) return;
    this.loading = true;
    this.auth.forgotPassword(this.email).subscribe({
      next: () => {
        this.loading = false;
        this.sent = true;
        this.snack.open('OTP sent! Check your email (including spam folder).', 'OK', { duration: 6000 });
        this.router.navigate(['/reset-password'], { queryParams: { email: this.email } });
      },
      error: (e) => {
        this.loading = false;
        let msg: string;
        if (e?.status === 503 || e?.status === 0) {
          msg = 'Server is starting up — please wait 30 seconds and try again.';
        } else if (e?.status === 500) {
          msg = 'Email service is temporarily unavailable. Please try again in a few minutes.';
        } else {
          msg = e?.error?.message ?? 'Failed to send OTP. Check your email address and try again.';
        }
        this.snack.open(msg, 'Close', { duration: 7000 });
      }
    });
  }
}
