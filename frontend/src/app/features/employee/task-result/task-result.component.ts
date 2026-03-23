import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { SubmissionDto, QuestionDto, MCQOptionDto } from '../../../core/models';

@Component({ selector: 'app-task-result', standalone: false, templateUrl: './task-result.component.html', styleUrl: './task-result.component.scss' })
export class TaskResultComponent implements OnInit {
  submission?: SubmissionDto;
  questions: QuestionDto[] = [];
  loading = true;

  constructor(private route: ActivatedRoute, private api: ApiService) {}

  ngOnInit(): void {
    const taskId = this.route.snapshot.paramMap.get('id')!;
    this.api.getMySubmission(taskId).subscribe({
      next: s => {
        this.submission = s;
        // Load with includeAnswers=true so we can show correct MCQ answers
        this.api.getQuestions(taskId, true).subscribe({
          next: q => { this.questions = q; this.loading = false; },
          error: () => this.loading = false
        });
      },
      error: () => this.loading = false
    });
  }

  getQuestion(qId: string): QuestionDto | undefined {
    return this.questions.find(q => q.id === qId);
  }

  /** For MCQ: resolve the selected option ID → option text */
  getSelectedOption(q: QuestionDto, answerText: string | undefined): MCQOptionDto | undefined {
    return q.options?.find(o => o.id === answerText);
  }

  /** For MCQ: get the correct option */
  getCorrectOption(q: QuestionDto): MCQOptionDto | undefined {
    return q.options?.find(o => o.isCorrect);
  }

  isMcqCorrect(q: QuestionDto, answerText: string | undefined): boolean {
    const correct = this.getCorrectOption(q);
    return !!correct && correct.id === answerText;
  }
}
