import { Component } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, Validators, ValidationErrors } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const np = group.get('newPassword')?.value;
  const cp = group.get('confirmPassword')?.value;
  return np && cp && np !== cp ? { passwordsMismatch: true } : null;
}

@Component({
  selector: 'app-change-password-dialog',
  standalone: false,
  template: `
    <h2 mat-dialog-title>Change Password</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="cpw-form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Current Password</mat-label>
          <input matInput formControlName="currentPassword" [type]="hideOld ? 'password' : 'text'">
          <button mat-icon-button matSuffix type="button" (click)="hideOld = !hideOld">
            <mat-icon>{{ hideOld ? 'visibility_off' : 'visibility' }}</mat-icon>
          </button>
          <mat-error *ngIf="form.get('currentPassword')?.hasError('required')">Required</mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>New Password</mat-label>
          <input matInput formControlName="newPassword" [type]="hideNew ? 'password' : 'text'">
          <button mat-icon-button matSuffix type="button" (click)="hideNew = !hideNew">
            <mat-icon>{{ hideNew ? 'visibility_off' : 'visibility' }}</mat-icon>
          </button>
          <mat-error *ngIf="form.get('newPassword')?.hasError('required')">Required</mat-error>
          <mat-error *ngIf="form.get('newPassword')?.hasError('minlength')">Minimum 8 characters</mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Confirm New Password</mat-label>
          <input matInput formControlName="confirmPassword" [type]="hideConfirm ? 'password' : 'text'">
          <button mat-icon-button matSuffix type="button" (click)="hideConfirm = !hideConfirm">
            <mat-icon>{{ hideConfirm ? 'visibility_off' : 'visibility' }}</mat-icon>
          </button>
          <mat-error *ngIf="form.get('confirmPassword')?.hasError('required')">Required</mat-error>
        </mat-form-field>

        <mat-error *ngIf="form.hasError('passwordsMismatch') && form.get('confirmPassword')?.touched" class="mismatch-error">
          Passwords do not match
        </mat-error>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close()">Cancel</button>
      <button mat-raised-button color="primary" (click)="submit()" [disabled]="form.invalid || saving">
        <mat-spinner *ngIf="saving" diameter="18" style="display:inline-block;margin-right:6px"></mat-spinner>
        {{ saving ? 'Saving...' : 'Change Password' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .cpw-form { display: flex; flex-direction: column; gap: 4px; padding-top: 4px; min-width: 340px; }
    .full-width { width: 100%; }
    .mismatch-error { font-size: 0.75rem; margin-top: -8px; margin-bottom: 4px; }
  `]
})
export class ChangePasswordDialogComponent {
  form: FormGroup;
  saving = false;
  hideOld = true;
  hideNew = true;
  hideConfirm = true;

  constructor(
    public dialogRef: MatDialogRef<ChangePasswordDialogComponent>,
    private fb: FormBuilder,
    private auth: AuthService,
    private snack: MatSnackBar
  ) {
    this.form = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required]
    }, { validators: passwordsMatch });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving = true;
    const { currentPassword, newPassword } = this.form.getRawValue();
    const trimmedCurrent = currentPassword?.trim();
    const trimmedNew = newPassword?.trim();
    if (!trimmedCurrent || !trimmedNew) { this.saving = false; return; }
    this.auth.changePassword(trimmedCurrent, trimmedNew).subscribe({
      next: () => {
        this.snack.open('Password changed successfully.', '', { duration: 3000 });
        this.dialogRef.close(true);
        this.saving = false;
      },
      error: (err) => {
        this.snack.open(err?.error?.message ?? 'Failed to change password.', '', { duration: 4000 });
        this.saving = false;
      }
    });
  }
}
