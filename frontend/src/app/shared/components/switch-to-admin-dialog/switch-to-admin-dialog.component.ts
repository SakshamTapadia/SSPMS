import { Component, Inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-switch-to-admin-dialog',
  standalone: false,
  template: `
    <h2 mat-dialog-title>
      <mat-icon style="vertical-align:middle;margin-right:8px;color:var(--c-primary)">admin_panel_settings</mat-icon>
      Switch to Admin Console
    </h2>
    <mat-dialog-content>
      <p style="color:var(--c-text-2);margin-bottom:16px">
        Enter your password to confirm your identity before accessing the Admin Console.
      </p>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Password</mat-label>
          <input matInput formControlName="password" [type]="hide ? 'password' : 'text'" (keydown.enter)="submit()">
          <button mat-icon-button matSuffix type="button" (click)="hide = !hide">
            <mat-icon>{{ hide ? 'visibility_off' : 'visibility' }}</mat-icon>
          </button>
          <mat-error *ngIf="form.get('password')?.hasError('required')">Password is required</mat-error>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close(false)">Cancel</button>
      <button mat-raised-button color="primary" (click)="submit()" [disabled]="form.invalid || verifying">
        <mat-spinner *ngIf="verifying" diameter="18" style="display:inline-block;margin-right:6px"></mat-spinner>
        {{ verifying ? 'Verifying...' : 'Switch to Admin' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .full-width { width: 100%; min-width: 340px; }
  `]
})
export class SwitchToAdminDialogComponent {
  form: FormGroup;
  hide = true;
  verifying = false;

  constructor(
    public dialogRef: MatDialogRef<SwitchToAdminDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public email: string,
    private fb: FormBuilder,
    private auth: AuthService,
    private snack: MatSnackBar
  ) {
    this.form = this.fb.group({ password: ['', Validators.required] });
  }

  submit(): void {
    if (this.form.invalid || this.verifying) return;
    this.verifying = true;
    const password = this.form.get('password')!.value;
    this.auth.login({ email: this.email, password }).subscribe({
      next: () => { this.verifying = false; this.dialogRef.close(true); },
      error: () => {
        this.verifying = false;
        this.snack.open('Incorrect password.', 'Close', { duration: 3000 });
      }
    });
  }
}
