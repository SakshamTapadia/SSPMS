import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LayoutComponent } from './layout/layout.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { UserListComponent } from './user-list/user-list.component';
import { ClassListComponent } from './class-list/class-list.component';
import { AuditLogComponent } from './audit-log/audit-log.component';
import { ReportsComponent } from './reports/reports.component';
import { ClassDetailComponent } from '../trainer/class-detail/class-detail.component';
import { TaskListComponent } from '../trainer/task-list/task-list.component';
import { TaskDetailComponent } from '../trainer/task-detail/task-detail.component';
import { TaskEvaluateComponent } from '../trainer/task-evaluate/task-evaluate.component';

const routes: Routes = [
  {
    path: '', component: LayoutComponent, children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'users', component: UserListComponent },
      { path: 'classes', component: ClassListComponent },
      { path: 'classes/:id', component: ClassDetailComponent },
      { path: 'tasks', component: TaskListComponent },
      { path: 'tasks/:id', component: TaskDetailComponent },
      { path: 'tasks/:id/evaluate', component: TaskEvaluateComponent },
      { path: 'audit-log', component: AuditLogComponent },
      { path: 'reports', component: ReportsComponent }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule {}
