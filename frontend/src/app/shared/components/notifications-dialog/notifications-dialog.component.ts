import { Component, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { ApiService } from '../../../core/services/api.service';
import { NotificationDto } from '../../../core/models';

@Component({
  selector: 'app-notifications-dialog',
  standalone: false,
  template: `
    <h2 mat-dialog-title>Notifications</h2>
    <mat-dialog-content class="notif-content">
      <div *ngIf="loading" style="text-align:center;padding:24px">
        <mat-spinner diameter="32"></mat-spinner>
      </div>
      <div *ngIf="!loading && items.length === 0" class="notif-empty">
        <mat-icon>notifications_none</mat-icon>
        <p>No notifications</p>
      </div>
      <div class="notif-list" *ngIf="!loading && items.length > 0">
        <div class="notif-item" *ngFor="let n of items" [class.notif-unread]="!n.isRead" [class.notif-read]="n.isRead">
          <div class="notif-body">
            <strong>{{ n.title }}</strong>
            <p>{{ n.body }}</p>
            <span class="notif-time">{{ n.createdAt | date:'dd MMM, HH:mm' }}</span>
          </div>
          <button mat-icon-button *ngIf="!n.isRead" (click)="markRead(n)" title="Mark as read">
            <mat-icon style="font-size:18px;width:18px;height:18px;line-height:18px">check_circle_outline</mat-icon>
          </button>
        </div>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button *ngIf="unreadCount > 0" (click)="markAll()">Mark all read</button>
      <button mat-button (click)="dialogRef.close()">Close</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .notif-content { min-width: 360px; max-height: 460px; overflow-y: auto; padding: 0 4px; }
    .notif-empty { text-align: center; padding: 32px; color: #888; }
    .notif-empty mat-icon { font-size: 48px; width: 48px; height: 48px; display: block; margin: 0 auto 8px; }
    .notif-list { display: flex; flex-direction: column; gap: 2px; }
    .notif-item { display: flex; align-items: center; gap: 8px; padding: 10px 8px; border-radius: 8px; border-bottom: 1px solid #f1f5f9; }
    .notif-item:last-child { border-bottom: none; }
    .notif-unread { background: rgba(99,102,241,0.06); border-left: 3px solid #6366f1; }
    .notif-read { border-left: 3px solid transparent; }
    .notif-body { flex: 1; min-width: 0; }
    .notif-body strong { font-size: 0.875rem; display: block; margin-bottom: 2px; }
    .notif-body p { margin: 0 0 4px; font-size: 0.8125rem; color: #555; line-height: 1.4; }
    .notif-time { font-size: 0.75rem; color: #94a3b8; }
  `]
})
export class NotificationsDialogComponent implements OnInit {
  items: NotificationDto[] = [];
  loading = true;

  get unreadCount(): number { return this.items.filter(n => !n.isRead).length; }

  constructor(public dialogRef: MatDialogRef<NotificationsDialogComponent>, private api: ApiService) {}

  ngOnInit(): void {
    this.api.getNotifications().subscribe({
      next: ns => { this.items = (ns ?? []).sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()); this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  markRead(n: NotificationDto): void { this.api.markNotificationRead(n.id).subscribe(() => n.isRead = true); }

  markAll(): void { this.api.markAllRead().subscribe(() => this.items.forEach(n => n.isRead = true)); }
}
