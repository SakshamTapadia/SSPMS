import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { GoogleAuthService } from '../../../core/services/google-auth.service';

declare const google: any;

@Component({
  selector: 'app-login',
  standalone: false,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit, OnDestroy {
  form: FormGroup;
  otpForm: FormGroup;
  loading = false;
  showTotp = false;
  hidePass = true;
  pendingVerificationEmail = '';
  otpLoading = false;
  resendLoading = false;
  private gsiRetryTimer?: ReturnType<typeof setTimeout>;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private signalR: SignalRService,
    private router: Router,
    private route: ActivatedRoute,
    private snack: MatSnackBar,
    private googleAuth: GoogleAuthService
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
      totpCode: ['']
    });
    this.otpForm = this.fb.group({
      otp: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(6)]]
    });
  }

  ngOnInit(): void {
    // If already authenticated, skip the login page entirely
    const u = this.auth.currentUser;
    if (u) {
      const dest = u.role === 'Admin' ? '/admin/dashboard' : u.role === 'Trainer' ? '/trainer/dashboard' : '/employee/dashboard';
      this.router.navigateByUrl(dest, { replaceUrl: true });
      return;
    }
    this.initGoogleSignIn();
  }

  ngOnDestroy(): void {
    clearTimeout(this.gsiRetryTimer);
  }

  private initGoogleSignIn(): void {
    if (typeof google === 'undefined') {
      this.gsiRetryTimer = setTimeout(() => this.initGoogleSignIn(), 500);
      return;
    }
    this.googleAuth.initialize((response) => this.handleGoogleCallback(response));
    this.googleAuth.renderButton('google-signin-btn', { theme: 'outline', size: 'large', width: 360, text: 'signin_with' });
  }

  private handleGoogleCallback(response: { credential: string }): void {
    this.loading = true;
    this.auth.googleLogin(response.credential).subscribe({
      next: res => {
        this.signalR.connectNotifications();
        const returnUrl = this.route.snapshot.queryParams['returnUrl'];
        const role = res.user.role;
        const dest = returnUrl ?? (role === 'Admin' ? '/admin/dashboard' : role === 'Trainer' ? '/trainer/dashboard' : '/employee/dashboard');
        this.router.navigateByUrl(dest);
      },
      error: err => {
        this.loading = false;
        this.snack.open(err.error?.message ?? 'Google sign-in failed', 'Close', { duration: 4000 });
      }
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const { email, password, totpCode } = this.form.value;
    this.auth.login({ email: email!, password: password!, totpCode: totpCode || undefined }).subscribe({
      next: res => {
        if (res.requiresVerification) {
          this.loading = false;
          this.pendingVerificationEmail = res.email ?? email;
          return;
        }
        this.signalR.connectNotifications();
        const returnUrl = this.route.snapshot.queryParams['returnUrl'];
        const role = res.user!.role;
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

  submitOtp(): void {
    if (this.otpForm.invalid) return;
    this.otpLoading = true;
    this.auth.verifyEmail(this.pendingVerificationEmail, this.otpForm.value.otp).subscribe({
      next: res => {
        this.signalR.connectNotifications();
        const role = res.user.role;
        this.router.navigateByUrl(role === 'Admin' ? '/admin/dashboard' : role === 'Trainer' ? '/trainer/dashboard' : '/employee/dashboard');
      },
      error: err => {
        this.otpLoading = false;
        this.snack.open(err.error?.message ?? 'Invalid code', 'Close', { duration: 4000 });
      }
    });
  }

  resendCode(): void {
    this.resendLoading = true;
    this.auth.resendVerification(this.pendingVerificationEmail).subscribe({
      next: () => { this.resendLoading = false; this.snack.open('Verification code resent!', 'OK', { duration: 3000 }); },
      error: () => { this.resendLoading = false; }
    });
  }

  backToLogin(): void {
    this.pendingVerificationEmail = '';
    this.otpForm.reset();
  }
}
