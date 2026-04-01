import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { switchMap, map } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { ClassDto, UserDto, TaskDto } from '../../../core/models';

@Component({
  selector: 'app-class-detail',
  standalone: false,
  templateUrl: './class-detail.component.html',
  styleUrl: './class-detail.component.scss'
})
export class ClassDetailComponent implements OnInit {
  cls: ClassDto | null = null;
  employees: UserDto[] = [];
  allEmployees: UserDto[] = [];
  tasks: TaskDto[] = [];
  loading = true;
  showAddForm = false;
  showEditForm = false;
  adding = false;
  saving = false;
  addForm: FormGroup;
  editForm: FormGroup;
  displayedColumns = ['name', 'email', 'actions'];
  taskColumns = ['title', 'status', 'startAt', 'endAt', 'actions'];
  searchEmployees = '';

  get filteredAllEmployees(): UserDto[] {
    const q = this.searchEmployees.toLowerCase();
    return this.allEmployees.filter(e =>
      !this.employees.some(en => en.id === e.id) &&
      (e.name.toLowerCase().includes(q) || e.email.toLowerCase().includes(q))
    );
  }

  get portalBase(): string {
    return this.router.url.startsWith('/admin') ? '/admin' : '/trainer';
  }

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService,
    private auth: AuthService,
    private fb: FormBuilder,
    private snack: MatSnackBar
  ) {
    this.addForm = this.fb.group({
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });
    this.editForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: [''],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      skillTags: ['']
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.api.getClass(id).subscribe({
      next: c => { this.cls = c; this.loading = false; },
      error: () => { this.loading = false; }
    });
    this.api.getClassEmployees(id).subscribe({
      next: e => this.employees = e as UserDto[]
    });
    this.api.getUsers(1, 200, 'Employee').subscribe({
      next: r => this.allEmployees = r.items
    });
    this.api.getTasks(id).subscribe({
      next: t => this.tasks = t
    });
  }

  goToTask(taskId: string): void {
    this.router.navigate([this.portalBase, 'tasks', taskId]);
  }

  createTask(): void {
    this.router.navigate([this.portalBase, 'tasks', 'new'], { queryParams: { classId: this.cls!.id } });
  }

  toggleAdd(): void {
    this.showAddForm = !this.showAddForm;
    if (this.showAddForm) this.showEditForm = false;
    this.searchEmployees = '';
    this.addForm.reset();
  }

  toggleEdit(): void {
    this.showEditForm = !this.showEditForm;
    if (this.showEditForm) {
      this.showAddForm = false;
      this.editForm.patchValue({
        name: this.cls!.name,
        description: this.cls!.description ?? '',
        startDate: this.cls!.startDate ? new Date(this.cls!.startDate) : null,
        endDate: this.cls!.endDate ? new Date(this.cls!.endDate) : null,
        skillTags: this.cls!.skillTags ?? ''
      });
    }
  }

  saveEdit(): void {
    if (this.editForm.invalid) { this.editForm.markAllAsTouched(); return; }
    this.saving = true;
    const v = this.editForm.value;
    const req = {
      name: v.name,
      description: v.description || undefined,
      startDate: this.toDateOnly(v.startDate),
      endDate: this.toDateOnly(v.endDate),
      skillTags: v.skillTags ? v.skillTags.split(',').map((s: string) => s.trim()).filter(Boolean).join(',') : undefined,
      trainerId: this.cls!.trainerId   // preserve existing trainer
    };
    this.api.updateClass(this.cls!.id, req as any).subscribe({
      next: updated => {
        this.cls = updated;
        this.showEditForm = false;
        this.snack.open('Class updated!', '', { duration: 2500 });
        this.saving = false;
      },
      error: err => {
        this.snack.open(err?.error?.message ?? 'Failed to update class.', 'Close', { duration: 3000 });
        this.saving = false;
      }
    });
  }

  private toDateOnly(date: any): string {
    if (!date) return '';
    const d = new Date(date);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  enrollExisting(employee: UserDto): void {
    if (!this.cls) return;
    this.api.enrollEmployee(this.cls.id, employee.id).subscribe({
      next: () => {
        this.employees = [...this.employees, employee];
        this.snack.open(`${employee.name} added to class.`, '', { duration: 2500 });
      },
      error: err => this.snack.open(err?.error?.message ?? 'Failed to add employee.', '', { duration: 3000 })
    });
  }

  createAndEnroll(): void {
    if (this.addForm.invalid || !this.cls) return;
    this.adding = true;
    const v = this.addForm.value;
    const classId = this.cls!.id;

    // Admin: POST /users (Admin-only) then POST /classes/{id}/enroll (Admin+Trainer)
    // Trainer: POST /users/trainer/employees (Trainer-only, creates + enrolls in one call)
    const create$ = this.auth.role === 'Admin'
      ? this.api.createUser({ name: v.name, email: v.email, role: 'Employee', password: v.password }).pipe(
          switchMap(user => this.api.enrollEmployee(classId, user.id).pipe(map(() => user)))
        )
      : this.api.createEmployeeByTrainer({ name: v.name, email: v.email, role: 'Employee', password: v.password, classId });

    create$.subscribe({
      next: user => {
        this.employees = [...this.employees, user];
        this.snack.open(`${user.name} created and added.`, '', { duration: 2500 });
        this.addForm.reset();
        this.adding = false;
      },
      error: err => {
        this.snack.open(err?.error?.message ?? 'Failed to create employee.', 'Close', { duration: 4000 });
        this.adding = false;
      }
    });
  }

  removeEmployee(emp: UserDto): void {
    if (!this.cls) return;
    this.api.removeEmployee(this.cls.id, emp.id).subscribe({
      next: () => {
        this.employees = this.employees.filter(e => e.id !== emp.id);
        this.snack.open(`${emp.name} removed.`, '', { duration: 2000 });
      },
      error: err => this.snack.open(err?.error?.message ?? 'Failed to remove.', '', { duration: 3000 })
    });
  }
}
