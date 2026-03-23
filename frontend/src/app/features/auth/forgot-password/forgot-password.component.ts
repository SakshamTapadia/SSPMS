import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({ selector: 'app-forgot-password', standalone: false, templateUrl: './forgot-password.component.html', styleUrl: './forgot-password.component.scss' })
export class ForgotPasswordComponent {
  email = '';
  loading = false;
  constructor(private auth: AuthService, private router: Router, private snack: MatSnackBar) {}
  send(): void {
    this.loading = true;
    this.auth.forgotPassword(this.email).subscribe({ next: () => { this.snack.open('OTP sent to your email', 'OK', { duration: 4000 }); this.router.navigate(['/reset-password'], { queryParams: { email: this.email } }); }, error: () => { this.snack.open('Error sending OTP', 'Close', { duration: 4000 }); this.loading = false; } });
  }
}
