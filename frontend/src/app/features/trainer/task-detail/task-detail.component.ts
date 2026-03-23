import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ClassDto, TaskDto, QuestionDto, QuestionType } from '../../../core/models';

@Component({
  selector: 'app-task-detail',
  standalone: false,
  templateUrl: './task-detail.component.html',
  styleUrl: './task-detail.component.scss'
})
export class TaskDetailComponent implements OnInit, OnDestroy {
  isNew = false;
  taskId = '';
  task: TaskDto | null = null;
  questions: QuestionDto[] = [];
  classes: ClassDto[] = [];
  loading = true;
  saving = false;
  publishing = false;

  taskForm: FormGroup;
  showQForm = false;
  editingQId = '';
  savingQ = false;
  qForm: FormGroup;

  readonly questionTypes: QuestionType[] = ['MCQ', 'Code', 'Assessment', 'Link'];
  readonly languages = ['javascript', 'python', 'java', 'csharp', 'cpp', 'sql', 'other'];

  get totalMarks(): number { return this.questions.reduce((s, q) => s + q.marks, 0); }
  get optionsArray(): FormArray { return this.qForm.get('options') as FormArray; }
  get isDraft(): boolean { return this.isNew || this.task?.status === 'Draft'; }

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService,
    private fb: FormBuilder,
    private snack: MatSnackBar
  ) {
    this.taskForm = this.fb.group({
      classId: ['', Validators.required],
      title: ['', [Validators.required, Validators.maxLength(200)]],
      description: [''],
      instructions: [''],
      startAt: ['', Validators.required],
      endAt: ['', Validators.required],
      durationMinutes: [60, [Validators.required, Validators.min(1)]]
    });

    this.qForm = this.fb.group({
      type: ['MCQ', Validators.required],
      stem: ['', Validators.required],
      marks: [10, [Validators.required, Validators.min(1)]],
      language: ['javascript'],
      options: this.fb.array([])
    });
  }

  private routeSub?: Subscription;

  ngOnInit(): void {
    this.api.getClasses().subscribe({ next: c => this.classes = c, error: () => {} });

    // Use paramMap observable so Angular doesn't get stuck when navigating
    // from /tasks/new → /tasks/:id (same component, route reused)
    this.routeSub = this.route.paramMap.subscribe(params => {
      const id = params.get('id')!;
      this.isNew = id === 'new';
      this.saving = false;
      this.publishing = false;
      this.showQForm = false;
      this.questions = [];
      this.task = null;
      this.clearOptions();

      if (!this.isNew) {
        this.taskId = id;
        this.loading = true;
        this.taskForm.enable();
        this.taskForm.get('classId')?.disable();
        this.api.getTask(id).subscribe({
          next: t => {
            this.task = t;
            this.taskForm.patchValue({
              classId: t.classId,
              title: t.title,
              description: t.description ?? '',
              instructions: t.instructions ?? '',
              startAt: this.toLocalDatetime(t.startAt),
              endAt: this.toLocalDatetime(t.endAt),
              durationMinutes: t.durationMinutes
            });
            if (t.status !== 'Draft') this.taskForm.disable();
            this.loading = false;
            this.loadQuestions();
          },
          error: () => {
            this.loading = false;
            this.snack.open('Task not found.', 'Close', { duration: 3000 });
            this.router.navigate(['/trainer/tasks']);
          }
        });
      } else {
        this.taskId = '';
        this.taskForm.reset({ durationMinutes: 60 });
        this.taskForm.enable();
        this.loading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  loadQuestions(): void {
    this.api.getQuestions(this.taskId, true).subscribe({ next: q => this.questions = q, error: () => {} });
  }

  saveTask(): void {
    if (this.taskForm.invalid) return;
    this.saving = true;
    const v = this.taskForm.getRawValue();
    const common = {
      title: v.title,
      description: v.description || undefined,
      instructions: v.instructions || undefined,
      startAt: new Date(v.startAt).toISOString(),
      endAt: new Date(v.endAt).toISOString(),
      durationMinutes: +v.durationMinutes
    };

    if (this.isNew) {
      this.api.createTask({ ...common, classId: v.classId }).subscribe({
        next: t => {
          this.snack.open('Task created! Add questions below.', '', { duration: 3000 });
          this.router.navigate(['/trainer/tasks', t.id]);
        },
        error: err => {
          this.snack.open(err?.error?.message ?? 'Failed to create task.', 'Close', { duration: 4000 });
          this.saving = false;
        }
      });
    } else {
      this.api.updateTask(this.taskId, common).subscribe({
        next: t => { this.task = t; this.snack.open('Saved!', '', { duration: 2000 }); this.saving = false; },
        error: err => {
          this.snack.open(err?.error?.message ?? 'Failed to save.', 'Close', { duration: 4000 });
          this.saving = false;
        }
      });
    }
  }

  publish(): void {
    if (!this.questions.length) {
      this.snack.open('Add at least one question before publishing.', 'Close', { duration: 3000 });
      return;
    }
    if (!confirm('Publish this task? All enrolled employees will be notified.')) return;
    this.publishing = true;
    this.api.publishTask(this.taskId).subscribe({
      next: () => {
        if (this.task) this.task = { ...this.task, status: 'Published' };
        this.taskForm.disable();
        this.snack.open('Task published!', '', { duration: 2500 });
        this.publishing = false;
      },
      error: err => {
        this.snack.open(err?.error?.message ?? 'Failed to publish.', 'Close', { duration: 4000 });
        this.publishing = false;
      }
    });
  }

  // ── Question form ────────────────────────────────────────────────────────

  openAddQuestion(): void {
    this.editingQId = '';
    this.clearOptions();
    this.qForm.reset({ type: 'MCQ', stem: '', marks: 10, language: 'javascript' });
    this.addOption(); this.addOption();
    this.showQForm = true;
    setTimeout(() => document.querySelector('.q-form-panel')?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 50);
  }

  openEditQuestion(q: QuestionDto): void {
    this.editingQId = q.id;
    this.clearOptions();
    this.qForm.patchValue({ type: q.type, stem: q.stem, marks: q.marks, language: q.language ?? 'javascript' });
    if (q.type === 'MCQ' && q.options?.length) {
      q.options.forEach(o => this.optionsArray.push(this.fb.group({ optionText: [o.optionText, Validators.required], isCorrect: [o.isCorrect ?? false] })));
    } else if (q.type === 'MCQ') {
      this.addOption(); this.addOption();
    }
    this.showQForm = true;
  }

  closeQForm(): void { this.showQForm = false; this.editingQId = ''; }

  addOption(): void { this.optionsArray.push(this.fb.group({ optionText: ['', Validators.required], isCorrect: [false] })); }
  removeOption(i: number): void { if (this.optionsArray.length > 2) this.optionsArray.removeAt(i); }
  clearOptions(): void { while (this.optionsArray.length) this.optionsArray.removeAt(0); }

  onTypeChange(): void {
    if (this.qForm.get('type')?.value === 'MCQ') { this.clearOptions(); this.addOption(); this.addOption(); }
    else { this.clearOptions(); }
  }

  saveQuestion(): void {
    if (this.qForm.invalid) return;
    this.savingQ = true;
    const v = this.qForm.value;
    const existingQ = this.questions.find(q => q.id === this.editingQId);
    const req = {
      type: v.type as QuestionType,
      stem: v.stem,
      marks: +v.marks,
      orderIndex: existingQ?.orderIndex ?? this.questions.length + 1,
      language: v.type === 'Code' ? v.language : undefined,
      options: v.type === 'MCQ' ? v.options : undefined
    };

    const obs = this.editingQId
      ? this.api.updateQuestion(this.taskId, this.editingQId, req)
      : this.api.addQuestion(this.taskId, req);

    obs.subscribe({
      next: () => {
        this.loadQuestions();
        this.closeQForm();
        this.savingQ = false;
      },
      error: err => {
        this.snack.open(err?.error?.message ?? 'Failed to save question.', 'Close', { duration: 4000 });
        this.savingQ = false;
      }
    });
  }

  deleteQuestion(qId: string): void {
    if (!confirm('Delete this question?')) return;
    this.api.deleteQuestion(this.taskId, qId).subscribe({
      next: () => {
        this.questions = this.questions.filter(q => q.id !== qId);
        this.snack.open('Question deleted.', '', { duration: 2000 });
      },
      error: err => this.snack.open(err?.error?.message ?? 'Failed to delete.', 'Close', { duration: 4000 })
    });
  }

  private toLocalDatetime(iso: string): string {
    if (!iso) return '';
    const d = new Date(iso);
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }
}
