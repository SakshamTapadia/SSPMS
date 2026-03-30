import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ApiService } from '../../../core/services/api.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { UserProfile } from '../../../core/models';
import { ChangePasswordDialogComponent } from '../../../shared/components/change-password-dialog/change-password-dialog.component';
import { NotificationsDialogComponent } from '../../../shared/components/notifications-dialog/notifications-dialog.component';
import { SwitchToAdminDialogComponent } from '../../../shared/components/switch-to-admin-dialog/switch-to-admin-dialog.component';
import { Router } from '@angular/router';

@Component({ selector: 'app-layout', standalone: false, templateUrl: './layout.component.html', styleUrl: './layout.component.scss' })
export class LayoutComponent implements OnInit {
  unreadCount = 0;
  sidebarOpen = false;
  user$: Observable<UserProfile | null>;

  constructor(public auth: AuthService, private api: ApiService, private signalR: SignalRService, private snack: MatSnackBar, private dialog: MatDialog, private router: Router) {
    this.user$ = auth.user$;
  }

  ngOnInit(): void {
    this.api.getNotifications().subscribe(ns => this.unreadCount = (ns ?? []).filter(n => !n.isRead).length);
    this.signalR.notification$.subscribe(n => { this.unreadCount++; this.snack.open(n.title, 'View', { duration: 4000 }); });
  }

  openChangePassword(): void { this.dialog.open(ChangePasswordDialogComponent, { width: '420px' }); }

  openNotifications(): void { this.dialog.open(NotificationsDialogComponent, { width: '420px' }); }

  switchToAdmin(): void {
    const email = this.auth.currentUser?.email ?? '';
    this.dialog.open(SwitchToAdminDialogComponent, { width: '440px', data: email })
      .afterClosed().subscribe(confirmed => { if (confirmed) this.router.navigate(['/admin/dashboard']); });
  }

  logout(): void { this.auth.logout(); }
  toggleSidebar(): void { this.sidebarOpen = !this.sidebarOpen; }
  closeSidebar(): void { this.sidebarOpen = false; }
}
