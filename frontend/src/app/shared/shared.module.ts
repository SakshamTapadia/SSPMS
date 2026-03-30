import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChangePasswordDialogComponent } from './components/change-password-dialog/change-password-dialog.component';
import { NotificationsDialogComponent } from './components/notifications-dialog/notifications-dialog.component';
import { SwitchToAdminDialogComponent } from './components/switch-to-admin-dialog/switch-to-admin-dialog.component';
import { ClassDetailComponent } from '../features/trainer/class-detail/class-detail.component';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

// Angular Material
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatRadioModule } from '@angular/material/radio';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDividerModule } from '@angular/material/divider';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatButtonToggleModule } from '@angular/material/button-toggle';

const MATERIAL = [
  MatToolbarModule, MatSidenavModule, MatListModule, MatButtonModule, MatIconModule,
  MatCardModule, MatTableModule, MatPaginatorModule, MatSortModule,
  MatFormFieldModule, MatInputModule, MatSelectModule, MatDialogModule,
  MatSnackBarModule, MatProgressSpinnerModule, MatChipsModule, MatBadgeModule,
  MatMenuModule, MatTabsModule, MatTooltipModule, MatDatepickerModule,
  MatNativeDateModule, MatCheckboxModule, MatRadioModule, MatProgressBarModule,
  MatExpansionModule, MatDividerModule, MatGridListModule, MatButtonToggleModule
];

@NgModule({
  declarations: [ChangePasswordDialogComponent, NotificationsDialogComponent, SwitchToAdminDialogComponent, ClassDetailComponent],
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule, ...MATERIAL],
  exports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule, ...MATERIAL, ChangePasswordDialogComponent, NotificationsDialogComponent, SwitchToAdminDialogComponent, ClassDetailComponent]
})
export class SharedModule {}
