import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { ProctorService, MAX_TAB_SWITCHES } from '../../../core/services/proctor.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { SubmissionDto, QuestionDto } from '../../../core/models';
import { interval, Subscription } from 'rxjs';

export type ProctorState = 'requesting' | 'denied' | 'ready' | 'in-progress';

@Component({ selector: 'app-task-attempt', standalone: false, templateUrl: './task-attempt.component.html', styleUrl: './task-attempt.component.scss' })
export class TaskAttemptComponent implements OnInit, OnDestroy {
  taskId = '';
  submission?: SubmissionDto;
  questions: QuestionDto[] = [];
  answers: Record<string, string> = {};
  timeLeft = 0;
  submittedCount = 0;
  loading = true;
  submitting = false;

  proctorState: ProctorState = 'requesting';
  proctorError = '';
  violationWarning = false;

  readonly maxTabSwitches = MAX_TAB_SWITCHES;

  private timerSub?: Subscription;
  private signalRSub?: Subscription;
  private fullscreenCleanup?: () => void;
  private visibilityCleanup?: () => void;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService,
    private signalR: SignalRService,
    public proctor: ProctorService,
    private snack: MatSnackBar
  ) {}

  async ngOnInit(): Promise<void> {
    this.taskId = this.route.snapshot.paramMap.get('id')!;
    await this.initProctoring();
  }

  async initProctoring(): Promise<void> {
    this.proctorState = 'requesting';
    const result = await this.proctor.requestPermissions();
    if (!result.granted) {
      this.proctorState = 'denied';
      this.proctorError = result.error ?? 'Camera and microphone access are required to begin.';
      return;
    }

    try {
      await this.proctor.enterFullscreen();
    } catch {
      // Fullscreen requires a user gesture in some browsers; proceed and warn.
    }

    // Fullscreen exit listener
    this.fullscreenCleanup = this.proctor.onFullscreenChange(() => {
      if (!this.proctor.isFullscreen()) {
        this.proctor.violationCount++;
        this.violationWarning = true;
        this.snack
          .open(`Proctoring violation #${this.proctor.violationCount}: fullscreen exited.`, 'Re-enter fullscreen', { duration: 0 })
          .onAction()
          .subscribe(() => {
            this.proctor.enterFullscreen().catch(() => {});
            this.violationWarning = false;
          });
      } else {
        this.violationWarning = false;
      }
    });

    // Tab/screen switch listener
    this.visibilityCleanup = this.proctor.onVisibilityChange(() => {
      const count = this.proctor.recordTabSwitch();
      const remaining = this.maxTabSwitches - count;

      if (remaining > 0) {
        this.snack.open(
          `Warning: screen switched (${count}/${this.maxTabSwitches}). ${remaining} attempt${remaining !== 1 ? 's' : ''} remaining before auto-submit.`,
          'Dismiss',
          { duration: 6000 }
        );
      } else {
        // 3rd switch — malpractice
        this.triggerMalpracticeSubmit(count);
      }
    });

    this.proctorState = 'ready';
    this.loadSubmission();
  }

  private loadSubmission(): void {
    this.api.startSubmission(this.taskId).subscribe({
      next: sub => {
        this.submission = sub;
        sub.answers.forEach(a => this.answers[a.questionId] = a.answerText ?? '');
        this.api.getQuestions(this.taskId).subscribe(q => {
          this.questions = q;
          this.loading = false;
          this.proctorState = 'in-progress';
          setTimeout(() => this.attachCameraPreview(), 50);
        });
        this.startTimer(sub);
        this.signalR.connectSubmissions(this.taskId);
        this.signalRSub = this.signalR.submissionCount$.subscribe(d => { if (d.taskId === this.taskId) this.submittedCount = d.count; });
      },
      error: e => { this.snack.open(e.error?.message ?? 'Cannot start task', 'Close', { duration: 5000 }); this.router.navigate(['/employee/tasks']); }
    });
  }

  private attachCameraPreview(): void {
    const stream = this.proctor.getMediaStream();
    const vid = document.getElementById('camera-pip') as HTMLVideoElement | null;
    if (!stream || !vid) return;
    vid.srcObject = stream;
    vid.muted = true;
    vid.play().catch(() => {});
  }

  private startTimer(sub: SubmissionDto): void {
    const endAt = new Date(sub.taskEndAt).getTime();
    this.timeLeft = Math.max(0, Math.floor((endAt - Date.now()) / 1000));
    this.timerSub = interval(1000).subscribe(() => {
      if (this.timeLeft > 0) this.timeLeft--;
      else this.autoSubmit();
    });
  }

  get timeDisplay(): string {
    const h = Math.floor(this.timeLeft / 3600);
    const m = Math.floor((this.timeLeft % 3600) / 60);
    const s = this.timeLeft % 60;
    return `${h.toString().padStart(2,'0')}:${m.toString().padStart(2,'0')}:${s.toString().padStart(2,'0')}`;
  }

  get tabSwitchCount(): number { return this.proctor.tabSwitchCount; }
  get tabSwitchesRemaining(): number { return Math.max(0, this.maxTabSwitches - this.tabSwitchCount); }

  saveDraft(): void {
    if (!this.submission) return;
    const req = { answers: Object.entries(this.answers).map(([questionId, answerText]) => ({ questionId, answerText })) };
    this.api.saveDraft(this.submission.id, req).subscribe({ next: () => this.snack.open('Draft saved', 'OK', { duration: 2000 }) });
  }

  submit(): void {
    if (!this.submission) return;
    if (!confirm('Submit your answers? This cannot be undone.')) return;
    this.submitting = true;
    this.saveDraftFirst().then(() => {
      this.api.submitSubmission(this.submission!.id).subscribe({
        next: () => { this.snack.open('Submitted!', 'OK', { duration: 3000 }); this.router.navigate(['/employee/tasks', this.taskId, 'result']); },
        error: e => { this.snack.open(e.error?.message ?? 'Error', 'Close', { duration: 4000 }); this.submitting = false; }
      });
    });
  }

  private triggerMalpracticeSubmit(switchCount: number): void {
    if (!this.submission || this.submitting) return;
    this.submitting = true;
    this.timerSub?.unsubscribe();
    this.snack.open('Malpractice detected: assessment auto-submitted.', 'Close', { duration: 0 });
    this.api.malpracticeSubmit(this.submission.id, switchCount).subscribe({
      next: () => this.router.navigate(['/employee/tasks', this.taskId, 'result']),
      error: () => this.router.navigate(['/employee/tasks', this.taskId, 'result'])
    });
  }

  private async saveDraftFirst(): Promise<void> {
    return new Promise(resolve => {
      if (!this.submission) { resolve(); return; }
      const req = { answers: Object.entries(this.answers).map(([questionId, answerText]) => ({ questionId, answerText })) };
      this.api.saveDraft(this.submission.id, req).subscribe({ next: () => resolve(), error: () => resolve() });
    });
  }

  private autoSubmit(): void {
    this.timerSub?.unsubscribe();
    this.snack.open('Time up! Auto-submitting...', 'OK', { duration: 5000 });
    if (this.submission) this.api.submitSubmission(this.submission.id).subscribe({ next: () => this.router.navigate(['/employee/tasks', this.taskId, 'result']) });
  }

  get violationCount(): number { return this.proctor.violationCount; }

  ngOnDestroy(): void {
    this.timerSub?.unsubscribe();
    this.signalRSub?.unsubscribe();
    this.signalR.disconnectSubmissions();
    this.fullscreenCleanup?.();
    this.visibilityCleanup?.();
    this.proctor.cleanup();
  }
}
