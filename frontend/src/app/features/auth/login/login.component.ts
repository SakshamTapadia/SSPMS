import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';

@Component({
  selector: 'app-login',
  standalone: false,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  form: FormGroup;
  loading = false;
  showTotp = false;
  hidePass = true;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private signalR: SignalRService,
    private router: Router,
    private route: ActivatedRoute,
    private snack: MatSnackBar
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
      totpCode: ['']
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const { email, password, totpCode } = this.form.value;
    this.auth.login({ email: email!, password: password!, totpCode: totpCode || undefined }).subscribe({
      next: res => {
        this.signalR.connectNotifications();
        const returnUrl = this.route.snapshot.queryParams['returnUrl'];
        const role = res.user.role;
        const dest = returnUrl ?? (role === 'Admin' ? '/admin/dashboard' : role === 'Trainer' ? '/trainer/dashboard' : '/employee/dashboard');
        this.router.navigateByUrl(dest);
      },
      error: err => {
        this.loading = false;
        const msg = err.error?.message ?? 'Login failed';
        if (msg.toLowerCase().includes('2fa') || msg.toLowerCase().includes('totp')) {
          this.showTotp = true;
          this.snack.open('Enter your 2FA code', 'OK', { duration: 4000 });
        } else {
          this.snack.open(msg, 'Close', { duration: 4000 });
        }
      }
    });
  }
}
