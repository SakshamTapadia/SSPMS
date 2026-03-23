import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { LeaderboardEntry } from '../../../core/models';

@Component({ selector: 'app-leaderboard', standalone: false, templateUrl: './leaderboard.component.html', styleUrl: './leaderboard.component.scss' })
export class LeaderboardComponent implements OnInit {
  entries: LeaderboardEntry[] = [];
  period = 'all';
  myId: string;
  loading = true;
  classId = '';
  noClass = false;

  constructor(private api: ApiService, private auth: AuthService) {
    this.myId = auth.userId;
  }

  ngOnInit(): void {
    this.api.getMyClass().subscribe({
      next: cls => {
        if (cls) { this.classId = cls.id; this.load(); }
        else { this.noClass = true; this.loading = false; }
      },
      error: () => { this.noClass = true; this.loading = false; }
    });
  }

  load(): void {
    this.loading = true;
    this.api.getClassLeaderboard(this.classId, this.period).subscribe({
      next: e => { this.entries = e; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }
}
