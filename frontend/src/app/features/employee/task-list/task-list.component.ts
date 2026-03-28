import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import { TaskDto } from '../../../core/models';

@Component({ selector: 'app-task-list', standalone: false, templateUrl: './task-list.component.html', styleUrl: './task-list.component.scss' })
export class TaskListComponent implements OnInit {
  tasks: TaskDto[] = [];
  loading = true;
  submittedTaskIds = new Set<string>();

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getMyTasks().subscribe({
      next: tasks => {
        this.tasks = tasks;
        // For each open task, check if already submitted so we hide the Start Task button
        tasks.forEach(t => {
          if (this.isOpen(t)) {
            this.api.getMySubmission(t.id).subscribe({
              next: sub => { if (sub?.status && sub.status !== 'Draft') this.submittedTaskIds.add(t.id); },
              error: () => {} // 404 means no submission yet — fine
            });
          }
        });
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  isOpen(t: TaskDto): boolean { const now = new Date(); return new Date(t.startAt) <= now && now <= new Date(t.endAt); }
  isNotStarted(t: TaskDto): boolean { return new Date(t.startAt) > new Date(); }
  hasSubmitted(t: TaskDto): boolean { return this.submittedTaskIds.has(t.id); }
  canAttempt(t: TaskDto): boolean { return this.isOpen(t) && !this.hasSubmitted(t); }
  buttonLabel(t: TaskDto): string { if (this.isOpen(t)) return 'Attempt Now'; if (this.isNotStarted(t)) return 'Not Started Yet'; return 'Closed'; }
}
