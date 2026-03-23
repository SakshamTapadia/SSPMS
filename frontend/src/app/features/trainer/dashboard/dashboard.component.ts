import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import { ClassDto, TaskDto } from '../../../core/models';

@Component({ selector: 'app-dashboard', standalone: false, templateUrl: './dashboard.component.html', styleUrl: './dashboard.component.scss' })
export class DashboardComponent implements OnInit {
  classes: ClassDto[] = [];
  recentTasks: TaskDto[] = [];
  loading = true;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getClasses().subscribe(c => { this.classes = c; this.loading = false; });
    this.api.getTasks().subscribe(t => { this.recentTasks = t; });
  }

  get totalEmployees(): number { return this.classes.reduce((s, c) => s + c.employeeCount, 0); }
  get totalTasks(): number { return this.classes.reduce((s, c) => s + c.taskCount, 0); }
  get openTasks(): number { return this.recentTasks.filter(t => t.status === 'Published').length; }
}
