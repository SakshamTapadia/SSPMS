import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import { DashboardStatsDto } from '../../../core/models';

const BADGE_META: Record<string, { icon: string; gradient: string }> = {
  'Perfect Score':  { icon: 'military_tech',         gradient: 'linear-gradient(135deg,#fde68a,#f59e0b,#d97706)' },
  'Speed Demon':    { icon: 'flash_on',              gradient: 'linear-gradient(135deg,#a78bfa,#7c3aed)' },
  'Early Bird':     { icon: 'wb_sunny',              gradient: 'linear-gradient(135deg,#fcd34d,#f59e0b)' },
  'Consistent':     { icon: 'event_repeat',          gradient: 'linear-gradient(135deg,#67e8f9,#0284c7)' },
  'Comeback King':  { icon: 'trending_up',           gradient: 'linear-gradient(135deg,#4ade80,#16a34a)' },
  'Streak Master':  { icon: 'local_fire_department', gradient: 'linear-gradient(135deg,#fb923c,#dc2626)' },
};
const DEFAULT_BADGE_META = { icon: 'workspace_premium', gradient: 'linear-gradient(135deg,#fbbf24,#d97706)' };

@Component({ selector: 'app-dashboard', standalone: false, templateUrl: './dashboard.component.html', styleUrl: './dashboard.component.scss' })
export class DashboardComponent implements OnInit {
  stats?: DashboardStatsDto;
  loading = true;
  constructor(private api: ApiService) {}
  ngOnInit(): void {
    this.api.getMyDashboard().subscribe({ next: s => { this.stats = s; this.loading = false; }, error: () => this.loading = false });
  }

  refreshStats(): void {
    this.api.getMyDashboard().subscribe({ next: s => { this.stats = s; }, error: () => {} });
  }

  badgeIcon(name: string): string { return (BADGE_META[name] ?? DEFAULT_BADGE_META).icon; }
  badgeGradient(name: string): string { return (BADGE_META[name] ?? DEFAULT_BADGE_META).gradient; }
}
