import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { SubmissionDto, SubmissionSummary, QuestionDto, TaskBlindSpotReport, CodeSimilarityReport, TaskResultsGrid, GridAnswerCell } from '../../../core/models';
import { MatSnackBar } from '@angular/material/snack-bar';

type MainTab = 'evaluate' | 'overview' | 'analytics';

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
  resultsGrid?: TaskResultsGrid;
  analyticsTab: 'blind-spots' | 'similarity' = 'blind-spots';
  mainTab: MainTab = 'evaluate';
  analyticsLoading = false;
  gridLoading = false;
  sortBy: 'accuracy' | 'score' | 'name' = 'accuracy';
  sortAsc = false;
  modalParticipant?: GridParticipantRow;

  constructor(private route: ActivatedRoute, private api: ApiService, private snack: MatSnackBar) {}

  ngOnInit(): void {
    this.taskId = this.route.snapshot.paramMap.get('id')!;
    this.api.getTaskSubmissions(this.taskId).subscribe(s => this.submissions = s);
    this.api.getQuestions(this.taskId, true).subscribe(q => this.questions = q);
  }

  switchTab(tab: MainTab): void {
    this.mainTab = tab;
    if (tab === 'overview' && !this.resultsGrid) this.loadResultsGrid();
    if (tab === 'analytics' && !this.blindSpots) this.loadAnalytics();
  }

  loadResultsGrid(): void {
    this.gridLoading = true;
    this.api.getTaskResultsGrid(this.taskId).subscribe({
      next: r => { this.resultsGrid = r; this.gridLoading = false; },
      error: () => this.gridLoading = false
    });
  }

  loadAnalytics(): void {
    this.analyticsLoading = true;
    this.api.getTaskBlindSpots(this.taskId).subscribe({ next: r => { this.blindSpots = r; this.analyticsLoading = false; }, error: () => this.analyticsLoading = false });
    this.api.getCodeSimilarity(this.taskId).subscribe({ next: r => { this.similarity = r; }, error: () => {} });
  }

  passRateClass(r: number): string { return r >= 0.7 ? 'good' : r >= 0.5 ? 'warn' : 'blind'; }
  similarityClass(s: number): string { return s >= 0.72 ? 'high' : s >= 0.5 ? 'med' : 'low'; }
  riskClass(r: string): string { return r === 'High' ? 'badge--danger' : r === 'Medium' ? 'badge--warning' : 'badge--success'; }
  accuracyColor(pct: number): string { return pct >= 70 ? '#16a34a' : pct >= 50 ? '#d97706' : '#dc2626'; }
  accuracyBg(pct: number): string { return pct >= 70 ? '#f0fdf4' : pct >= 50 ? '#fffbeb' : '#fef2f2'; }
  cellClass(isCorrect: boolean | null): string {
    if (isCorrect === null) return 'cell-neutral';
    return isCorrect ? 'cell-correct' : 'cell-wrong';
  }

  findAnswer(answers: GridAnswerCell[], questionId: string): GridAnswerCell | undefined {
    return answers.find(a => a.questionId === questionId);
  }

  isValidUrl(val: string): boolean {
    try { new URL(val); return true; } catch { return false; }
  }

  get totalMarks(): number {
    return this.resultsGrid?.questions.reduce((s, q) => s + q.marks, 0) ?? 0;
  }

  get sortedParticipants(): GridParticipantRow[] {
    if (!this.resultsGrid) return [];
    return [...this.resultsGrid.participants].sort((a, b) => {
      let cmp = 0;
      if (this.sortBy === 'accuracy') cmp = a.accuracyPercent - b.accuracyPercent;
      else if (this.sortBy === 'score') cmp = a.score - b.score;
      else cmp = a.employeeName.localeCompare(b.employeeName);
      return this.sortAsc ? cmp : -cmp;
    });
  }

  toggleSort(by: 'accuracy' | 'score' | 'name'): void {
    if (this.sortBy === by) { this.sortAsc = !this.sortAsc; }
    else { this.sortBy = by; this.sortAsc = false; }
  }

  openModal(p: GridParticipantRow): void { this.modalParticipant = p; }
  closeModal(): void { this.modalParticipant = undefined; }

  evaluateFromGrid(p: GridParticipantRow): void {
    const sub = this.submissions.find(s => s.employeeId === p.employeeId);
    if (sub) { this.select(sub); this.mainTab = 'evaluate'; }
  }

  correctCount(p: GridParticipantRow): number { return p.answers.filter(a => a.isCorrect === true).length; }
  wrongCount(p: GridParticipantRow): number { return p.answers.filter(a => a.isCorrect === false).length; }

  select(sub: SubmissionSummary): void {
    this.mainTab = 'evaluate';
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
