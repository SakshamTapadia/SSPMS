import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import { TaskDto } from '../../../core/models';

@Component({ selector: 'app-task-list', standalone: false, templateUrl: './task-list.component.html', styleUrl: './task-list.component.scss' })
export class TaskListComponent implements OnInit {
  tasks: TaskDto[] = [];
  loading = true;
  constructor(private api: ApiService) {}
  ngOnInit(): void { this.api.getMyTasks().subscribe({ next: t => { this.tasks = t; this.loading = false; }, error: () => this.loading = false }); }
  isOpen(t: TaskDto): boolean { const now = new Date(); return new Date(t.startAt) <= now && now <= new Date(t.endAt); }
  isNotStarted(t: TaskDto): boolean { return new Date(t.startAt) > new Date(); }
  buttonLabel(t: TaskDto): string { if (this.isOpen(t)) return 'Attempt Now'; if (this.isNotStarted(t)) return 'Not Started Yet'; return 'Closed'; }
}
