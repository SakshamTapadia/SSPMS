import { Component, OnInit, OnDestroy } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ApiService } from '../../../core/services/api.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { UserProfile } from '../../../core/models';
import { ChangePasswordDialogComponent } from '../../../shared/components/change-password-dialog/change-password-dialog.component';
import { NotificationsDialogComponent } from '../../../shared/components/notifications-dialog/notifications-dialog.component';

@Component({ selector: 'app-layout', standalone: false, templateUrl: './layout.component.html', styleUrl: './layout.component.scss' })
export class LayoutComponent implements OnInit, OnDestroy {
  unreadCount = 0;
  sidebarOpen = false;
  user$: Observable<UserProfile | null>;

  constructor(public auth: AuthService, private api: ApiService, private signalR: SignalRService, private snack: MatSnackBar, private dialog: MatDialog) {
    this.user$ = auth.user$;
  }

  ngOnInit(): void {
    this.api.getNotifications().subscribe(ns => this.unreadCount = (ns ?? []).filter(n => !n.isRead).length);
    this.signalR.notification$.subscribe(n => { this.unreadCount++; this.snack.open(n.title, 'View', { duration: 4000 }); });
  }

  openChangePassword(): void { this.dialog.open(ChangePasswordDialogComponent, { width: '420px' }); }

  openNotifications(): void { this.dialog.open(NotificationsDialogComponent, { width: '420px' }); }

  logout(): void { this.auth.logout(); }
  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
    document.body.style.overflow = this.sidebarOpen ? 'hidden' : '';
  }
  closeSidebar(): void {
    this.sidebarOpen = false;
    document.body.style.overflow = '';
  }
  ngOnDestroy(): void { document.body.style.overflow = ''; }
}
