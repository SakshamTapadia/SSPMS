import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';

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
export class RegisterComponent {
  form: FormGroup;
  loading = false;
  hidePass = true;
  hideConfirm = true;
  submitted = false;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private signalR: SignalRService,
    private router: Router,
    private snack: MatSnackBar
  ) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8),
        Validators.pattern(/^(?=.*[A-Z])(?=.*[0-9])(?=.*[^A-Za-z0-9])/)
      ]],
      confirmPassword: ['', Validators.required]
    }, { validators: passwordsMatch });
  }

  submit(): void {
    this.submitted = true;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      // Show explicit snackbar so the user knows the form has errors
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
      next: () => {
        this.signalR.connectNotifications();
        this.router.navigateByUrl('/employee/dashboard');
      },
      error: err => {
        this.loading = false;
        this.snack.open(err.error?.message ?? 'Registration failed', 'Close', { duration: 5000 });
      }
    });
  }
}
