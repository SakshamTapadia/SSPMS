import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';

const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: '',
    loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule)
  },
  {
    path: 'admin',
    loadChildren: () => import('./features/admin/admin.module').then(m => m.AdminModule),
    canActivate: [AuthGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'trainer',
    loadChildren: () => import('./features/trainer/trainer.module').then(m => m.TrainerModule),
    canActivate: [AuthGuard],
    data: { roles: ['Admin', 'Trainer'] }
  },
  {
    path: 'employee',
    loadChildren: () => import('./features/employee/employee.module').then(m => m.EmployeeModule),
    canActivate: [AuthGuard],
    data: { roles: ['Employee'] }
  },
  { path: 'unauthorized', loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule) },
  { path: '**', redirectTo: 'login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { scrollPositionRestoration: 'top' })],
  exports: [RouterModule]
})
export class AppRoutingModule {}
