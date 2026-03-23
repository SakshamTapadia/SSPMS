import { Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NotificationDto } from '../models';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {
  private notifConn?: signalR.HubConnection;
  private submissionConn?: signalR.HubConnection;

  readonly notification$ = new Subject<NotificationDto>();
  readonly announcement$ = new Subject<any>();
  readonly submissionCount$ = new Subject<{ taskId: string; count: number }>();

  constructor(private auth: AuthService) {}

  connectNotifications(): void {
    if (this.notifConn) return;
    this.notifConn = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/notifications`, {
        accessTokenFactory: () => this.auth.getAccessToken() ?? ''
      })
      .withAutomaticReconnect()
      .build();

    this.notifConn.on('ReceiveNotification', (n: NotificationDto) => this.notification$.next(n));
    this.notifConn.on('ReceiveAnnouncement', (a: any) => this.announcement$.next(a));
    this.notifConn.start().catch(console.error);
  }

  connectSubmissions(taskId: string): void {
    if (this.submissionConn) return;
    this.submissionConn = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/submissions`, {
        accessTokenFactory: () => this.auth.getAccessToken() ?? ''
      })
      .withAutomaticReconnect()
      .build();

    this.submissionConn.on('SubmissionCountUpdated', (data: { taskId: string; count: number }) => this.submissionCount$.next(data));
    this.submissionConn.start().then(() => {
      this.submissionConn?.invoke('JoinTaskGroup', taskId).catch(console.error);
    }).catch(console.error);
  }

  disconnectSubmissions(): void {
    this.submissionConn?.stop();
    this.submissionConn = undefined;
  }

  joinClassGroup(classId: string): void {
    this.notifConn?.invoke('JoinClassGroup', classId).catch(console.error);
  }

  ngOnDestroy(): void {
    this.notifConn?.stop();
    this.submissionConn?.stop();
  }
}
