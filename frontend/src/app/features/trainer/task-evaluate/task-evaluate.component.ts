import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { SubmissionDto, SubmissionSummary, QuestionDto, TaskBlindSpotReport, CodeSimilarityReport } from '../../../core/models';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({ selector: 'app-task-evaluate', standalone: false, templateUrl: './task-evaluate.component.html', styleUrl: './task-evaluate.component.scss' })
export class TaskEvaluateComponent implements OnInit {
  taskId = '';
  submissions: SubmissionSummary[] = [];
  questions: QuestionDto[] = [];
  selected?: SubmissionDto;
  scores: Record<string, number> = {};
  notes: Record<string, string> = {};
  flags: Record<string, boolean> = {};
  loading = false;
  blindSpots?: TaskBlindSpotReport;
  similarity?: CodeSimilarityReport;
  analyticsTab: 'blind-spots' | 'similarity' = 'blind-spots';
  analyticsLoading = false;

  constructor(private route: ActivatedRoute, private api: ApiService, private snack: MatSnackBar) {}

  ngOnInit(): void {
    this.taskId = this.route.snapshot.paramMap.get('id')!;
    this.api.getTaskSubmissions(this.taskId).subscribe(s => this.submissions = s);
    this.api.getQuestions(this.taskId, true).subscribe(q => this.questions = q);
  }

  loadAnalytics(): void {
    if (this.blindSpots) return;
    this.analyticsLoading = true;
    this.api.getTaskBlindSpots(this.taskId).subscribe({ next: r => { this.blindSpots = r; this.analyticsLoading = false; }, error: () => this.analyticsLoading = false });
    this.api.getCodeSimilarity(this.taskId).subscribe({ next: r => { this.similarity = r; }, error: () => {} });
  }

  passRateClass(r: number): string { return r >= 0.7 ? 'good' : r >= 0.5 ? 'warn' : 'blind'; }
  similarityClass(s: number): string { return s >= 0.72 ? 'high' : s >= 0.5 ? 'med' : 'low'; }
  riskClass(r: string): string { return r === 'High' ? 'badge--danger' : r === 'Medium' ? 'badge--warning' : 'badge--success'; }

  select(sub: SubmissionSummary): void {
    this.api.getSubmission(sub.id).subscribe(s => {
      this.selected = s;
      s.answers.forEach(a => {
        this.scores[a.id] = a.rawScore ?? 0;
        this.notes[a.id] = a.evaluatorNote ?? '';
        this.flags[a.id] = a.isPlagiarismFlag ?? false;
      });
    });
  }

  getQuestion(qId: string): QuestionDto | undefined { return this.questions.find(q => q.id === qId); }

  save(): void {
    if (!this.selected) return;
    this.loading = true;
    const answers = this.selected.answers.map(a => ({ answerId: a.id, rawScore: this.scores[a.id] ?? 0, evaluatorNote: this.notes[a.id], isPlagiarismFlag: this.flags[a.id] ?? false }));
    this.api.evaluateSubmission(this.selected.id, { answers }).subscribe({
      next: () => { this.snack.open('Evaluation saved!', 'OK', { duration: 3000 }); this.loading = false; this.api.getTaskSubmissions(this.taskId).subscribe(s => this.submissions = s); },
      error: () => { this.snack.open('Error saving', 'Close', { duration: 4000 }); this.loading = false; }
    });
  }

  bulkComplete(): void {
    if (!confirm('Mark all submitted as evaluated?')) return;
    this.api.bulkCompleteEvaluation(this.taskId).subscribe({ next: () => { this.snack.open('All marked evaluated!', 'OK', { duration: 3000 }); this.api.getTaskSubmissions(this.taskId).subscribe(s => this.submissions = s); } });
  }
}
