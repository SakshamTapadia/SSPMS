import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LayoutComponent } from './layout/layout.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { TaskListComponent } from './task-list/task-list.component';
import { TaskAttemptComponent } from './task-attempt/task-attempt.component';
import { TaskResultComponent } from './task-result/task-result.component';
import { LeaderboardComponent } from './leaderboard/leaderboard.component';
import { ReportsComponent } from './reports/reports.component';

const routes: Routes = [
  {
    path: '', component: LayoutComponent, children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'tasks', component: TaskListComponent },
      { path: 'tasks/:id/attempt', component: TaskAttemptComponent },
      { path: 'tasks/:id/result', component: TaskResultComponent },
      { path: 'leaderboard', component: LeaderboardComponent },
      { path: 'reports', component: ReportsComponent }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class EmployeeRoutingModule {}
