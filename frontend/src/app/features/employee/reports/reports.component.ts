import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import { EmployeeReportDto } from '../../../core/models';

@Component({ selector: 'app-reports', standalone: false, templateUrl: './reports.component.html', styleUrl: './reports.component.scss' })
export class ReportsComponent implements OnInit {
  report?: EmployeeReportDto;
  loading = true;
  constructor(private api: ApiService) {}
  ngOnInit(): void { this.api.getEmployeeReport().subscribe({ next: r => { this.report = r; this.loading = false; }, error: () => this.loading = false }); }
  downloadPdf(): void { this.api.exportEmployeeReportPdf().subscribe(b => this.api.downloadBlob(b, 'my-report.pdf')); }
}
