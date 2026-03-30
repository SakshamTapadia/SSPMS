import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({ selector: 'app-reset-password', standalone: false, templateUrl: './reset-password.component.html', styleUrl: './reset-password.component.scss' })
export class ResetPasswordComponent {
  email: string;
  otp = '';
  newPassword = '';
  confirmPassword = '';
  loading = false;
  resending = false;
  showPassword = false;
  showConfirm = false;
  step: 'verify' | 'done' = 'verify';

  constructor(private auth: AuthService, route: ActivatedRoute, private router: Router, private snack: MatSnackBar) {
    this.email = route.snapshot.queryParams['email'] ?? '';
  }

  resendOtp(): void {
    if (!this.email.trim()) { this.snack.open('Enter your email address first.', 'Close', { duration: 4000 }); return; }
    this.resending = true;
    this.auth.forgotPassword(this.email).subscribe({
      next: () => { this.resending = false; this.snack.open('New OTP sent! Check your inbox and spam folder.', 'OK', { duration: 6000 }); },
      error: (e) => { this.resending = false; this.snack.open(e?.error?.message ?? 'Failed to resend. Try again shortly.', 'Close', { duration: 6000 }); }
    });
  }

  reset(): void {
    if (!this.otp.trim() || !this.newPassword.trim()) return;
    if (this.newPassword !== this.confirmPassword) { this.snack.open('Passwords do not match.', 'Close', { duration: 4000 }); return; }
    if (this.newPassword.length < 8) { this.snack.open('Password must be at least 8 characters.', 'Close', { duration: 4000 }); return; }
    this.loading = true;
    this.auth.resetPassword(this.email, this.otp, this.newPassword).subscribe({
      next: () => {
        this.loading = false;
        this.step = 'done';
        this.snack.open('Password reset successfully!', 'OK', { duration: 5000 });
        setTimeout(() => this.router.navigate(['/login']), 2000);
      },
      error: e => {
        this.loading = false;
        this.snack.open(e.error?.message ?? 'Invalid or expired OTP. Request a new one.', 'Close', { duration: 6000 });
      }
    });
  }
}
