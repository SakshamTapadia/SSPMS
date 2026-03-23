import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';
import { ClassDto, UserDto } from '../../../core/models';

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
  loading = true;
  showAddForm = false;
  adding = false;
  addForm: FormGroup;
  displayedColumns = ['name', 'email', 'actions'];
  searchEmployees = '';

  get filteredAllEmployees(): UserDto[] {
    const q = this.searchEmployees.toLowerCase();
    return this.allEmployees.filter(e =>
      !this.employees.some(en => en.id === e.id) &&
      (e.name.toLowerCase().includes(q) || e.email.toLowerCase().includes(q))
    );
  }

  constructor(
    private route: ActivatedRoute,
    private api: ApiService,
    private fb: FormBuilder,
    private snack: MatSnackBar
  ) {
    this.addForm = this.fb.group({
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
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
  }

  toggleAdd(): void { this.showAddForm = !this.showAddForm; this.searchEmployees = ''; this.addForm.reset(); }

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
    this.api.createEmployeeByTrainer({ name: v.name, email: v.email, role: 'Employee', password: v.password }).subscribe({
      next: user => {
        this.employees = [...this.employees, user];
        this.snack.open(`${user.name} created and added.`, '', { duration: 2500 });
        this.addForm.reset();
        this.adding = false;
      },
      error: err => {
        this.snack.open(err?.error?.message ?? 'Failed to create employee.', '', { duration: 3000 });
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
