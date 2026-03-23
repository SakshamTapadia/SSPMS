import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';

@Component({ selector: 'app-dashboard', standalone: false, templateUrl: './dashboard.component.html', styleUrl: './dashboard.component.scss' })
export class DashboardComponent implements OnInit {
  stats: any = null;
  loading = true;
  constructor(private api: ApiService) {}
  ngOnInit(): void { this.api.getSystemStats().subscribe({ next: s => { this.stats = s; this.loading = false; }, error: () => this.loading = false }); }
}
