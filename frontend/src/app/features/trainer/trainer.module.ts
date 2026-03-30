import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { TrainerRoutingModule } from './trainer-routing.module';
import { SharedModule } from '../../shared/shared.module';
import { DashboardComponent } from './dashboard/dashboard.component';
import { ClassListComponent } from './class-list/class-list.component';
import { LeaderboardComponent } from './leaderboard/leaderboard.component';
import { ReportsComponent } from './reports/reports.component';
import { LayoutComponent } from './layout/layout.component';


@NgModule({
  declarations: [
    DashboardComponent,
    ClassListComponent,
    LeaderboardComponent,
    ReportsComponent,
    LayoutComponent
  ],
  imports: [
    CommonModule,
    TrainerRoutingModule,
    SharedModule
  ]
})
export class TrainerModule { }
