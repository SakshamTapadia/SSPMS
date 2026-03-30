import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { GoogleAuthService } from '../../../core/services/google-auth.service';

declare const google: any;

function passwordsMatch(g: AbstractControl) {
  return g.get('password')?.value === g.get('confirmPassword')?.value
    ? null : { mismatch: true };
}

@Component({
  selector: 'app-register',
  standalone: false,
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent implements OnInit, OnDestroy {
  form: FormGroup;
  otpForm: FormGroup;
  loading = false;
  hidePass = true;
  hideConfirm = true;
  submitted = false;
  pendingEmail = '';
  otpLoading = false;
  resendLoading = false;
  private gsiRetryTimer?: ReturnType<typeof setTimeout>;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private signalR: SignalRService,
    private router: Router,
    private snack: MatSnackBar,
    private googleAuth: GoogleAuthService
  ) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8),
        Validators.pattern(/^(?=.*[A-Z])(?=.*[0-9])(?=.*[^A-Za-z0-9])/)
      ]],
      confirmPassword: ['', Validators.required]
    }, { validators: passwordsMatch });

    this.otpForm = this.fb.group({
      otp: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(6)]]
    });
  }

  ngOnInit(): void {
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
    this.googleAuth.renderButton('google-register-btn', { theme: 'outline', size: 'large', width: 360, text: 'signup_with' });
  }

  private handleGoogleCallback(response: { credential: string }): void {
    this.loading = true;
    this.auth.googleLogin(response.credential).subscribe({
      next: res => {
        this.signalR.connectNotifications();
        this.router.navigateByUrl('/employee/dashboard');
      },
      error: err => {
        this.loading = false;
        this.snack.open(err.error?.message ?? 'Google sign-up failed', 'Close', { duration: 5000 });
      }
    });
  }

  submit(): void {
    this.submitted = true;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      const msgs: string[] = [];
      const pw = this.form.get('password');
      if (pw?.hasError('required')) msgs.push('Password is required');
      else if (pw?.hasError('minlength')) msgs.push('Password must be at least 8 characters');
      else if (pw?.hasError('pattern')) msgs.push('Password needs uppercase, number & symbol');
      if (this.form.hasError('mismatch')) msgs.push('Passwords do not match');
      if (this.form.get('name')?.invalid) msgs.push('Name is required');
      if (this.form.get('email')?.invalid) msgs.push('Valid email is required');
      this.snack.open(msgs[0] ?? 'Please fix the errors in the form', 'Close', { duration: 5000 });
      return;
    }
    this.loading = true;
    const { name, email, password } = this.form.value;
    this.auth.register({ name, email, password }).subscribe({
      next: res => {
        this.loading = false;
        if (res.requiresVerification) {
          this.pendingEmail = res.email ?? email;
          this.snack.open('Account created! Check your email for the verification code.', 'OK', { duration: 5000 });
        } else if (res.accessToken) {
          // Shouldn't happen but handle it
          this.auth.storeAuth(res as any);
          this.signalR.connectNotifications();
          this.router.navigateByUrl('/employee/dashboard');
        }
      },
      error: err => {
        this.loading = false;
        this.snack.open(err.error?.message ?? 'Registration failed', 'Close', { duration: 5000 });
      }
    });
  }

  submitOtp(): void {
    if (this.otpForm.invalid) return;
    this.otpLoading = true;
    this.auth.verifyEmail(this.pendingEmail, this.otpForm.value.otp).subscribe({
      next: res => {
        this.signalR.connectNotifications();
        this.router.navigateByUrl('/employee/dashboard');
      },
      error: err => {
        this.otpLoading = false;
        this.snack.open(err.error?.message ?? 'Invalid code', 'Close', { duration: 4000 });
      }
    });
  }

  resendCode(): void {
    this.resendLoading = true;
    this.auth.resendVerification(this.pendingEmail).subscribe({
      next: () => { this.resendLoading = false; this.snack.open('Code resent!', 'OK', { duration: 3000 }); },
      error: () => { this.resendLoading = false; }
    });
  }

  backToForm(): void {
    this.pendingEmail = '';
    this.otpForm.reset();
  }
}
