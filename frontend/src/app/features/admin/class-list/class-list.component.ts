import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';
import { ClassDto, UserDto } from '../../../core/models';

@Component({
  selector: 'app-class-list',
  standalone: false,
  templateUrl: './class-list.component.html',
  styleUrl: './class-list.component.scss'
})
export class ClassListComponent implements OnInit {
  classes: ClassDto[] = [];
  trainers: UserDto[] = [];
  loading = true;
  showCreateForm = false;
  saving = false;
  form: FormGroup;

  constructor(
    private api: ApiService,
    private fb: FormBuilder,
    private router: Router,
    private snack: MatSnackBar
  ) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: [''],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      skillTags: [''],
      trainerId: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.api.getClasses().subscribe({ next: c => { this.classes = c; this.loading = false; }, error: () => { this.loading = false; } });
    this.api.getTrainers().subscribe({ next: t => this.trainers = t, error: () => {} });
  }

  toggleCreate(): void {
    this.showCreateForm = !this.showCreateForm;
    if (!this.showCreateForm) { this.form.reset(); return; }
    if (!this.trainers.length) {
      this.api.getTrainers().subscribe({ next: t => this.trainers = t, error: () => {} });
    }
  }

  getTagsArray(tags?: string): string[] {
    return tags ? tags.split(',').map(t => t.trim()).filter(Boolean) : [];
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving = true;
    const v = this.form.value;
    const req = {
      name: v.name,
      description: v.description || undefined,
      startDate: this.toDateOnly(v.startDate),
      endDate: this.toDateOnly(v.endDate),
      skillTags: v.skillTags ? v.skillTags.split(',').map((s: string) => s.trim()).filter(Boolean).join(',') : undefined,
      trainerId: v.trainerId
    };
    this.api.createClass(req).subscribe({
      next: cls => {
        this.classes = [cls, ...this.classes];
        this.snack.open('Class created!', '', { duration: 2500 });
        this.showCreateForm = false;
        this.form.reset();
        this.saving = false;
      },
      error: err => {
        this.snack.open(err?.error?.message ?? 'Failed to create class.', '', { duration: 3000 });
        this.saving = false;
      }
    });
  }

  openDetail(cls: ClassDto): void { this.router.navigate(['/trainer/classes', cls.id]); }

  deleteClass(cls: ClassDto): void {
    if (!confirm(`Delete class "${cls.name}"? This will permanently delete all its tasks and submissions.`)) return;
    this.api.deleteClass(cls.id).subscribe({
      next: () => {
        this.classes = this.classes.filter(c => c.id !== cls.id);
        this.snack.open('Class deleted.', '', { duration: 2500 });
      },
      error: err => this.snack.open(err?.error?.message ?? 'Failed to delete class.', 'Close', { duration: 4000 })
    });
  }

  private toDateOnly(date: any): string {
    if (!date) return '';
    const d = new Date(date);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
