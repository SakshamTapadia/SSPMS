import { Component, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-audit-log',
  standalone: false,
  templateUrl: './audit-log.component.html',
  styleUrl: './audit-log.component.scss'
})
export class AuditLogComponent implements OnInit {
  logs: any[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 50;
  loading = true;
  actionCtrl = new FormControl('');
  displayedColumns = ['timestamp', 'user', 'action', 'entity', 'ip'];

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.load();
    this.actionCtrl.valueChanges.pipe(debounceTime(400), distinctUntilChanged()).subscribe(() => {
      this.page = 1; this.load();
    });
  }

  load(): void {
    this.loading = true;
    const action = this.actionCtrl.value || undefined;
    this.api.getAuditLog(this.page, action).subscribe({
      next: r => { this.logs = r.items; this.totalCount = r.totalCount; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  onPageChange(e: any): void { this.page = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); }

  badgeClass(action: string): string {
    if (action.includes('Login') || action.includes('Register')) return 'badge-auth';
    if (action.includes('Fail') || action.includes('Error')) return 'badge-error';
    if (action.includes('Delete') || action.includes('Deactivate')) return 'badge-warn';
    return 'badge-default';
  }
}
