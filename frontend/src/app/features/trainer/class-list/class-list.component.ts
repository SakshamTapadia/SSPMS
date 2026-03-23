import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { ClassDto } from '../../../core/models';

@Component({
  selector: 'app-class-list',
  standalone: false,
  templateUrl: './class-list.component.html',
  styleUrl: './class-list.component.scss'
})
export class ClassListComponent implements OnInit {
  classes: ClassDto[] = [];
  loading = true;
  showCreateForm = false;
  saving = false;
  form: FormGroup;

  constructor(
    private api: ApiService,
    private auth: AuthService,
    private fb: FormBuilder,
    private router: Router,
    private snack: MatSnackBar
  ) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: [''],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      skillTags: ['']
    });
  }

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.api.getClasses().subscribe({
      next: c => { this.classes = c; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  openDetail(id: string): void { this.router.navigate(['/trainer/classes', id]); }

  toggleCreate(): void { this.showCreateForm = !this.showCreateForm; if (!this.showCreateForm) this.form.reset(); }

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
      trainerId: this.auth.userId
    };
    this.api.createClass(req).subscribe({
      next: cls => {
        this.classes = [cls, ...this.classes];
        this.snack.open('Class created!', '', { duration: 2500 });
        this.showCreateForm = false;
        this.form.reset();
        this.saving = false;
      },
      error: (err) => {
        this.snack.open(err?.error?.message ?? 'Failed to create class.', '', { duration: 3000 });
        this.saving = false;
      }
    });
  }

  getTagsArray(tags?: string): string[] {
    return tags ? tags.split(',').map(t => t.trim()).filter(Boolean) : [];
  }

  private toDateOnly(date: any): string {
    if (!date) return '';
    const d = new Date(date);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
