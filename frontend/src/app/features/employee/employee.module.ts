import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { EmployeeRoutingModule } from './employee-routing.module';
import { SharedModule } from '../../shared/shared.module';
import { DashboardComponent } from './dashboard/dashboard.component';
import { TaskListComponent } from './task-list/task-list.component';
import { TaskAttemptComponent } from './task-attempt/task-attempt.component';
import { TaskResultComponent } from './task-result/task-result.component';
import { LeaderboardComponent } from './leaderboard/leaderboard.component';
import { ReportsComponent } from './reports/reports.component';
import { LayoutComponent } from './layout/layout.component';


@NgModule({
  declarations: [
    DashboardComponent,
    TaskListComponent,
    TaskAttemptComponent,
    TaskResultComponent,
    LeaderboardComponent,
    ReportsComponent,
    LayoutComponent
  ],
  imports: [
    CommonModule,
    EmployeeRoutingModule,
    SharedModule
  ]
})
export class EmployeeModule { }
