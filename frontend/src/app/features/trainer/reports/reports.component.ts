import { Component, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ClassDto, ClassReportDto } from '../../../core/models';

@Component({
  selector: 'app-reports',
  standalone: false,
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent implements OnInit {
  classes: ClassDto[] = [];
  selectedClass: ClassDto | null = null;
  report: ClassReportDto | null = null;
  loading = true;
  reportLoading = false;
  taskColumns = ['title', 'submitted', 'avgScore', 'completion'];

  private reportSub?: Subscription;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getClasses().subscribe({
      next: c => { this.classes = c; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  selectClass(cls: ClassDto): void {
    // Cancel any in-flight request so a stale response can't overwrite a newer selection
    this.reportSub?.unsubscribe();
    this.selectedClass = cls;
    this.report = null;
    this.reportLoading = true;
    this.reportSub = this.api.getClassReport(cls.id).subscribe({
      next: r => { this.report = r; this.reportLoading = false; },
      error: () => { this.reportLoading = false; }
    });
  }

  exportExcel(): void {
    if (!this.selectedClass) return;
    this.api.exportClassReportExcel(this.selectedClass.id).subscribe(blob =>
      this.api.downloadBlob(blob, `${this.selectedClass!.name}-report.xlsx`)
    );
  }

  completionPct(submitted: number, notSubmitted: number): string {
    const total = submitted + notSubmitted;
    return total ? ((submitted / total) * 100).toFixed(0) + '%' : '0%';
  }
}
