import { Component } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { MatDialog } from '@angular/material/dialog';
import { UserProfile } from '../../../core/models';
import { ChangePasswordDialogComponent } from '../../../shared/components/change-password-dialog/change-password-dialog.component';

@Component({ selector: 'app-layout', standalone: false, templateUrl: './layout.component.html', styleUrl: './layout.component.scss' })
export class LayoutComponent {
  sidebarOpen = false;
  user$: Observable<UserProfile | null>;

  constructor(public auth: AuthService, private dialog: MatDialog) {
    this.user$ = auth.user$;
  }

  openChangePassword(): void { this.dialog.open(ChangePasswordDialogComponent, { width: '420px' }); }

  logout(): void { this.auth.logout(); }
  toggleSidebar(): void { this.sidebarOpen = !this.sidebarOpen; }
  closeSidebar(): void { this.sidebarOpen = false; }
}
