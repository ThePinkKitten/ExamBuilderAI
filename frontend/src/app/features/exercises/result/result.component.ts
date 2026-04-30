import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { DecimalPipe } from '@angular/common';
import { ExerciseResultResponse } from '../../../shared/models/api.models';

@Component({
  selector: 'app-result',
  standalone: true,
  imports: [MatIconModule, MatButtonModule, DecimalPipe],
  template: `
    @if (result()) {
      <div class="page-container">
        <!-- Score Banner -->
        <div class="score-banner glass-card animate-fade-in"
             [class.excellent]="result()!.scorePercent >= 80"
             [class.good]="result()!.scorePercent >= 50 && result()!.scorePercent < 80"
             [class.needs-work]="result()!.scorePercent < 50">
          <div class="score-circle">
            <span class="score-value">{{ result()!.scorePercent | number:'1.0-0' }}%</span>
          </div>
          <div class="score-details">
            <h2>
              @if (result()!.scorePercent >= 80) { 🎉 Xuất sắc! }
              @else if (result()!.scorePercent >= 50) { 👍 Tốt lắm! }
              @else { 💪 Cần cố gắng thêm! }
            </h2>
            <p>{{ result()!.correctCount }}/{{ result()!.totalQuestions }} câu đúng
              · {{ formatTime(result()!.timeTakenSeconds) }}</p>
          </div>
        </div>

        <!-- Review Questions -->
        @if (result()!.sectionCode !== 'paragraph_writing') {
          <h3 class="review-title">📋 Chi tiết từng câu</h3>
          <div class="review-list">
            @for (q of getQuestions(); track q.id; let i = $index) {
              <div class="review-card glass-card"
                   [class.correct]="isCorrect(q)"
                   [class.incorrect]="!isCorrect(q)"
                   [style.animation-delay]="(i * 0.05) + 's'">
                <div class="review-header">
                  <span class="review-status">
                    @if (isCorrect(q)) {
                      <mat-icon>check_circle</mat-icon> Đúng
                    } @else {
                      <mat-icon>cancel</mat-icon> Sai
                    }
                  </span>
                  <span class="review-qnum">Câu {{ q.id }}</span>
                </div>

                @if (q.sentence || q.statement || q.question || q.prompt) {
                  <p class="review-question">{{ q.sentence || q.statement || q.question || q.prompt }}</p>
                }

                @if (!isCorrect(q) && q.options) {
                  <div class="answer-comparison">
                    <div class="your-answer">
                      <strong>Bạn chọn:</strong>
                      {{ getOptionLetter(getUserAnswer(q)) }}. {{ q.options[getUserAnswer(q)] }}
                    </div>
                    <div class="correct-answer">
                      <strong>Đáp án đúng:</strong>
                      {{ getOptionLetter(q.correctAnswer) }}. {{ q.options[q.correctAnswer] }}
                    </div>
                  </div>
                }

                @if (!isCorrect(q) && !q.options) {
                  <div class="answer-comparison">
                    <div class="your-answer">
                      <strong>Bạn trả lời:</strong> {{ getUserAnswerText(q) }}
                    </div>
                    <div class="correct-answer">
                      <strong>Đáp án đúng:</strong> {{ q.correctAnswer }}
                    </div>
                  </div>
                }

                @if (q.explanation) {
                  <div class="explanation">
                    <mat-icon>lightbulb</mat-icon>
                    <span>{{ q.explanation }}</span>
                  </div>
                }
              </div>
            }
          </div>
        } @else if (result()!.aiFeedback) {
          <div class="writing-feedback glass-card animate-fade-in">
            <h3><mat-icon>rate_review</mat-icon> AI Feedback</h3>
            <p class="feedback-text">{{ result()!.aiFeedback }}</p>
          </div>
        }

        <!-- Actions -->
        <div class="actions animate-fade-in">
          <button mat-flat-button class="accent-btn" (click)="generateNew()">
            <mat-icon>refresh</mat-icon> Tạo bài mới
          </button>
          <button mat-stroked-button (click)="goToDashboard()">
            <mat-icon>dashboard</mat-icon> Về Dashboard
          </button>
        </div>
      </div>
    }
  `,
  styles: [`
    .score-banner {
      display: flex;
      align-items: center;
      gap: 24px;
      margin-bottom: 32px;
      padding: 32px;

      &.excellent { border-left: 4px solid var(--success); }
      &.good { border-left: 4px solid var(--warning); }
      &.needs-work { border-left: 4px solid var(--danger); }
    }

    .score-circle {
      width: 80px;
      height: 80px;
      border-radius: 50%;
      background: var(--accent-gradient);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .score-value {
      font-size: 24px;
      font-weight: 700;
      color: white;
    }

    .score-details {
      h2 { font-size: 22px; margin-bottom: 4px; }
      p { color: var(--text-secondary); font-size: 14px; }
    }

    .review-title {
      font-size: 18px;
      margin-bottom: 16px;
    }

    .review-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
      margin-bottom: 32px;
      max-width: 800px;
    }

    .review-card {
      animation: fadeInUp 0.4s ease-out both;

      &.correct { border-left: 3px solid var(--success); }
      &.incorrect { border-left: 3px solid var(--danger); }
    }

    .review-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 8px;
    }

    .review-status {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 13px;
      font-weight: 600;

      .correct & { color: var(--success); }
      .incorrect & { color: var(--danger); }

      mat-icon { font-size: 18px; width: 18px; height: 18px; }
    }

    .review-qnum {
      font-size: 12px;
      color: var(--text-muted);
    }

    .review-question {
      font-size: 14px;
      margin-bottom: 12px;
      line-height: 1.5;
    }

    .answer-comparison {
      display: flex;
      flex-direction: column;
      gap: 6px;
      font-size: 13px;
      margin-bottom: 12px;

      .your-answer { color: var(--danger); }
      .correct-answer { color: var(--success); }
    }

    .explanation {
      display: flex;
      gap: 8px;
      padding: 10px;
      background: rgba(124, 58, 237, 0.1);
      border-radius: 8px;
      font-size: 13px;
      color: var(--text-secondary);
      line-height: 1.5;

      mat-icon {
        color: var(--warning);
        font-size: 18px;
        width: 18px;
        height: 18px;
        flex-shrink: 0;
        margin-top: 2px;
      }
    }

    .writing-feedback {
      margin-bottom: 32px;

      h3 {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-bottom: 16px;
        color: var(--accent-secondary);
      }

      .feedback-text {
        font-size: 14px;
        line-height: 1.8;
        color: var(--text-secondary);
      }
    }

    .actions {
      display: flex;
      gap: 16px;
      margin-bottom: 32px;
    }
  `]
})
export class ResultComponent implements OnInit {
  result = signal<ExerciseResultResponse | null>(null);

  constructor(private route: ActivatedRoute, private router: Router) {}

  ngOnInit() {
    const state = history.state;
    if (state?.result) {
      this.result.set(state.result);
    } else {
      this.router.navigate(['/dashboard']);
    }
  }

  getQuestions(): any[] {
    const content = this.result()?.fullContent;
    return content?.questions || content?.blanks || [];
  }

  isCorrect(q: any): boolean {
    const userAnswers = this.result()?.userAnswers;
    if (!userAnswers) return false;
    const userAnswer = userAnswers[q.id.toString()];

    if (q.type === 'true_false') return userAnswer === q.correctAnswer;
    if (typeof q.correctAnswer === 'number') return userAnswer === q.correctAnswer;
    if (typeof q.correctAnswer === 'string') {
      return (userAnswer || '').toString().trim().toLowerCase() === q.correctAnswer.trim().toLowerCase();
    }
    return false;
  }

  getUserAnswer(q: any): number {
    return this.result()?.userAnswers?.[q.id.toString()] ?? -1;
  }

  getUserAnswerText(q: any): string {
    return this.result()?.userAnswers?.[q.id.toString()] || '(không trả lời)';
  }

  getOptionLetter(index: number): string {
    if (index < 0) return '?';
    return String.fromCharCode(65 + index);
  }

  formatTime(seconds: number): string {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}m ${s}s`;
  }

  generateNew() {
    const section = this.result()?.sectionCode;
    this.router.navigate(['/exercise/generate'], {
      queryParams: section ? { section } : {}
    });
  }

  goToDashboard() {
    this.router.navigate(['/dashboard']);
  }
}
