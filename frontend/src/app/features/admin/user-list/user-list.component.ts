import { Component, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { UserDto, PagedResult } from '../../../core/models';

@Component({
  selector: 'app-user-list',
  standalone: false,
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss'
})
export class UserListComponent implements OnInit {
  result: PagedResult<UserDto> | null = null;
  loading = true;
  page = 1;
  pageSize = 20;
  searchCtrl = new FormControl('');
  roleFilter = '';
  activeFilter = '';
  displayedColumns = ['name', 'email', 'role', 'status', 'actions'];
  roleOptions = ['', 'Admin', 'Trainer', 'Employee'];
  changingRole: Record<string, boolean> = {};
  togglingStatus: Record<string, boolean> = {};

  constructor(private api: ApiService, private snack: MatSnackBar) {}

  ngOnInit(): void {
    this.load();
    this.searchCtrl.valueChanges.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => {
      this.page = 1; this.load();
    });
  }

  load(): void {
    this.loading = true;
    const search = this.searchCtrl.value || undefined;
    const isActive = this.activeFilter === '' ? undefined : this.activeFilter === 'true';
    this.api.getUsers(this.page, this.pageSize, this.roleFilter || undefined, search, isActive).subscribe({
      next: r => { this.result = r; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  onRoleFilterChange(): void { this.page = 1; this.load(); }
  onActiveFilterChange(): void { this.page = 1; this.load(); }

  onPageChange(event: any): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.load();
  }

  changeRole(user: UserDto, newRole: string): void {
    if (user.role === newRole || user.role === 'Admin') return;
    this.changingRole[user.id] = true;
    this.api.changeUserRole(user.id, newRole).subscribe({
      next: () => {
        user.role = newRole;
        this.snack.open(`Role changed to ${newRole}.`, '', { duration: 2000 });
        this.changingRole[user.id] = false;
      },
      error: err => {
        this.snack.open(err?.error?.message ?? 'Failed to change role.', '', { duration: 3000 });
        this.changingRole[user.id] = false;
      }
    });
  }

  toggleStatus(user: UserDto): void {
    this.togglingStatus[user.id] = true;
    const obs = user.isActive ? this.api.deactivateUser(user.id) : this.api.reactivateUser(user.id);
    obs.subscribe({
      next: () => {
        user.isActive = !user.isActive;
        this.snack.open(user.isActive ? 'User reactivated.' : 'User deactivated.', '', { duration: 2000 });
        this.togglingStatus[user.id] = false;
      },
      error: err => {
        this.snack.open(err?.error?.message ?? 'Failed.', '', { duration: 3000 });
        this.togglingStatus[user.id] = false;
      }
    });
  }
}
