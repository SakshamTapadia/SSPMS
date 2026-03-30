import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-reports',
  standalone: false,
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent implements OnInit {
  report: any = null;
  loading = true;
  classColumns = ['className', 'trainerName', 'employees', 'tasks', 'avgScore', 'completion', 'export'];

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getSystemReport().subscribe({
      next: r => { this.report = r; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  exportExcel(classId: string, className: string): void {
    this.api.exportClassReportExcel(classId).subscribe(blob =>
      this.api.downloadBlob(blob, `${className}-report.xlsx`)
    );
  }

  pct(v: number): string { return (v * 100).toFixed(1) + '%'; }
}
