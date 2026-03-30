import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { TaskDto, ClassDto } from '../../../core/models';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({ selector: 'app-task-list', standalone: false, templateUrl: './task-list.component.html', styleUrl: './task-list.component.scss' })
export class TaskListComponent implements OnInit {
  tasks: TaskDto[] = [];
  classes: ClassDto[] = [];
  selectedClass = '';
  loading = true;

  get portalBase(): string { return this.router.url.startsWith('/admin') ? '/admin' : '/trainer'; }

  constructor(private api: ApiService, private snack: MatSnackBar, private router: Router) {}

  ngOnInit(): void {
    this.api.getClasses().subscribe({ next: c => this.classes = c, error: () => {} });
    this.loadTasks();
  }

  loadTasks(): void {
    this.loading = true;
    this.api.getTasks(this.selectedClass || undefined).subscribe({ next: t => { this.tasks = t; this.loading = false; }, error: () => this.loading = false });
  }

  publish(id: string): void {
    this.api.publishTask(id).subscribe({ next: () => { this.snack.open('Task published!', 'OK', { duration: 3000 }); this.loadTasks(); }, error: e => this.snack.open(e.error?.message ?? 'Error', 'Close', { duration: 4000 }) });
  }

  duplicate(id: string): void {
    this.api.duplicateTask(id).subscribe({ next: () => { this.snack.open('Task duplicated!', 'OK', { duration: 3000 }); this.loadTasks(); } });
  }

  delete(id: string): void {
    if (!confirm('Delete this draft task?')) return;
    this.api.deleteTask(id).subscribe({ next: () => { this.snack.open('Deleted', 'OK', { duration: 3000 }); this.loadTasks(); } });
  }
}
