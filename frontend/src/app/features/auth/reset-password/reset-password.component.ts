import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({ selector: 'app-reset-password', standalone: false, templateUrl: './reset-password.component.html', styleUrl: './reset-password.component.scss' })
export class ResetPasswordComponent {
  email: string;
  otp = '';
  newPassword = '';
  loading = false;
  showPassword = false;

  constructor(private auth: AuthService, private route: ActivatedRoute, private router: Router, private snack: MatSnackBar) {
    this.email = route.snapshot.queryParams['email'] ?? '';
  }

  reset(): void {
    this.loading = true;
    this.auth.resetPassword(this.email, this.otp, this.newPassword).subscribe({
      next: () => { this.snack.open('Password reset! Please log in.', 'OK', { duration: 4000 }); this.router.navigate(['/login']); },
      error: e => { this.snack.open(e.error?.message ?? 'Error', 'Close', { duration: 4000 }); this.loading = false; }
    });
  }
}
