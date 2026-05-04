import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { DecimalPipe } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ExerciseResultResponse } from '../../../shared/models/api.models';
import { ExerciseService } from '../../../core/services/exercise.service';

@Component({
  selector: 'app-result',
  standalone: true,
  imports: [MatIconModule, MatButtonModule, DecimalPipe, MatProgressSpinnerModule],
  template: `
    @if (loading()) {
      <div class="loading-center">
        <mat-spinner diameter="48"></mat-spinner>
      </div>
    } @else if (result()) {
      <div class="page-container focus-page">
        <div class="master-wrapper">
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
                @if (result()!.scorePercent >= 80) { 🎉 Excellent! }
                @else if (result()!.scorePercent >= 50) { 👍 Great job! }
                @else { 💪 Keep going! }
              </h2>
              <p>{{ result()!.correctCount }}/{{ result()!.totalQuestions }} correct answers
                · {{ formatTime(result()!.timeTakenSeconds) }}</p>
            </div>
          </div>

          <!-- Review Questions -->
          @if (result()!.sectionCode !== 'paragraph_writing') {
            <h3 class="review-title">📋 Question details</h3>
            <div class="review-list">
              @for (q of getQuestions(); track q.id; let i = $index) {
                <div class="review-card glass-card"
                     [class.correct]="isCorrect(q)"
                     [class.incorrect]="!isCorrect(q)"
                     [style.animation-delay]="(i * 0.05) + 's'">
                  <div class="review-header">
                    <span class="review-status">
                      @if (isCorrect(q)) {
                        <mat-icon>check_circle</mat-icon> Correct
                      } @else {
                        <mat-icon>cancel</mat-icon> Incorrect
                      }
                    </span>
                    <span class="review-qnum">Question {{ q.id }}</span>
                  </div>

                  @if (q.sentence || q.statement || q.question || q.prompt) {
                    <p class="review-question">{{ q.sentence || q.statement || q.question || q.prompt }}</p>
                  }

                  @if (!isCorrect(q) && q.options) {
                  <div class="answer-comparison">
                    <div class="answer-line">
                      <span class="label">Your answer:</span>
                      <span class="value incorrect">
                        {{ getOptionLetter(getUserAnswer(q)) }}. 
                        <span [innerHTML]="formatOption(q.options[getUserAnswer(q)], q.underlinedParts?.[getUserAnswer(q)])"></span>
                      </span>
                    </div>
                    <div class="answer-line">
                      <span class="label">Correct answer:</span>
                      <span class="value correct">
                        {{ getOptionLetter(q.correctAnswer) }}. 
                        <span [innerHTML]="formatOption(q.options[q.correctAnswer], q.underlinedParts?.[q.correctAnswer])"></span>
                      </span>
                    </div>
                  </div>
                }

                @if (!isCorrect(q) && !q.options) {
                  <div class="answer-comparison">
                    <div class="answer-line">
                      <span class="label">Your response:</span>
                      <span class="value incorrect">{{ getUserAnswerText(q) }}</span>
                    </div>
                    <div class="answer-line">
                      <span class="label">Correct answer:</span>
                      <span class="value correct">{{ q.correctAnswer }}</span>
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
            <button mat-flat-button class="accent-btn primary-btn" (click)="generateNew()" [disabled]="actionLoading()">
              <mat-icon>refresh</mat-icon> New exercise
            </button>
            
            <button mat-stroked-button class="secondary-btn" (click)="reAnswer()" [disabled]="actionLoading()">
              @if (actionLoading()) {
                <mat-spinner diameter="20"></mat-spinner>
              } @else {
                <ng-container>
                  <mat-icon>replay</mat-icon> Re-answer (All)
                </ng-container>
              }
            </button>

            @if (result()!.scorePercent < 100 && result()!.sectionCode !== 'paragraph_writing') {
              <button mat-stroked-button color="warn" class="mistake-btn" (click)="fixMistakes()" [disabled]="actionLoading()">
                <mat-icon>build</mat-icon> Fix Mistakes
              </button>
            }

            <button mat-button class="text-btn" (click)="goToDashboard()" [disabled]="actionLoading()">
              <mat-icon>dashboard</mat-icon> Back to Dashboard
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .loading-center {
      display: flex;
      justify-content: center;
      padding: 80px 0;
    }

    .focus-page {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 32px 24px !important;
    }

    .master-wrapper {
      width: 100%;
      max-width: 800px;
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .score-banner {
      display: flex;
      align-items: center;
      gap: 24px;
      padding: 32px;
      border: 1px solid var(--border);
      
      &.excellent { border-color: rgba(16, 185, 129, 0.3); }
      &.good { border-color: rgba(245, 158, 11, 0.3); }
      &.needs-work { border-color: rgba(239, 68, 68, 0.3); }
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
      font-weight: 700;
      margin-top: 12px;
      margin-bottom: 24px; // Increased spacing
    }

    .review-list {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .review-card {
      animation: fadeInUp 0.4s ease-out both;
      border: 1px solid var(--border);

      &.correct { border-color: rgba(16, 185, 129, 0.2); }
      &.incorrect { border-color: rgba(239, 68, 68, 0.2); }
    }

    .review-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 12px;
    }

    .review-status {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 13px;
      font-weight: 700;

      .correct & { color: var(--success); }
      .incorrect & { color: var(--danger); }

      mat-icon { font-size: 18px; width: 18px; height: 18px; }
    }

    .review-qnum {
      font-size: 12px;
      font-weight: 600;
      color: var(--text-muted);
      text-transform: uppercase;
    }

    .review-question {
      font-size: 15px;
      margin-bottom: 16px;
      line-height: 1.6;
      color: #333;
    }

    .answer-comparison {
      display: flex;
      flex-direction: column;
      gap: 8px;
      font-size: 14px;
      margin-bottom: 16px;
      padding: 12px 16px;
      background: #f9fafb;
      border-radius: 12px;
      border: 1px solid #f1f5f9;

      .answer-line {
        display: flex;
        gap: 8px;
        
        .label {
          color: #4b5563;
          font-weight: 600;
          min-width: 110px;
        }
        
        .value {
          &.incorrect { color: var(--danger); font-weight: 500; }
          &.correct { color: var(--success); font-weight: 600; }
        }
      }
    }

    .explanation {
      display: flex;
      gap: 10px;
      padding: 12px 16px;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 10px;
      font-size: 13px;
      color: #475569;
      line-height: 1.6;

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
      border: 1px solid var(--border);

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
      align-items: center;
      justify-content: center;
      flex-wrap: wrap;
      gap: 12px;
      margin-top: 16px;
      margin-bottom: 40px;
    }

    .primary-btn {
      height: 48px;
      padding: 0 24px;
      border-radius: 12px;
      font-weight: 600;
      box-shadow: none !important;
    }

    .secondary-btn {
      height: 48px;
      padding: 0 20px;
      border-radius: 12px;
      font-weight: 600;
      border-color: var(--accent-primary) !important;
      color: var(--accent-primary) !important;
      
      &:hover {
        background: rgba(244, 63, 94, 0.05) !important;
      }
    }

    .mistake-btn {
      height: 48px;
      padding: 0 20px;
      border-radius: 12px;
      font-weight: 600;
      border-color: #ef4444 !important;
      color: #ef4444 !important;

      &:hover {
        background: rgba(239, 68, 68, 0.05) !important;
      }
    }

    .text-btn {
      &:hover { color: var(--text-primary); }
    }

    ::ng-deep .pronunciation-underline {
      text-decoration: underline;
      text-decoration-color: var(--accent-primary);
      text-decoration-thickness: 2px;
      font-weight: 700;
      color: var(--accent-primary);
    }
  `]
})
export class ResultComponent implements OnInit {
  result = signal<ExerciseResultResponse | null>(null);
  loading = signal<boolean>(false);
  actionLoading = signal<boolean>(false);

  constructor(
    private route: ActivatedRoute, 
    private router: Router,
    private exerciseService: ExerciseService
  ) {}

  ngOnInit() {
    const state = history.state;
    if (state?.result) {
      this.result.set(state.result);
    } else {
      // Try to fetch it using the route parameter
      this.route.paramMap.subscribe(params => {
        const id = Number(params.get('id'));
        if (id) {
          this.loading.set(true);
          this.exerciseService.getReview(id).subscribe({
            next: (res) => {
              this.result.set(res);
              this.loading.set(false);
            },
            error: () => {
              this.loading.set(false);
              this.router.navigate(['/dashboard']);
            }
          });
        } else {
          this.router.navigate(['/dashboard']);
        }
      });
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
    return this.result()?.userAnswers?.[q.id.toString()] || '(no answer)';
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

  formatOption(option: string, underlinedPart?: string): string {
    if (!underlinedPart) return option;
    
    const index = option.toLowerCase().indexOf(underlinedPart.toLowerCase());
    if (index === -1) return option; 
    
    const before = option.substring(0, index);
    const match = option.substring(index, index + underlinedPart.length);
    const after = option.substring(index + underlinedPart.length);
    
    return `${before}<u class="pronunciation-underline">${match}</u>${after}`;
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

  reAnswer() {
    const id = this.result()?.exerciseId;
    if (!id) return;
    
    this.actionLoading.set(true);
    this.exerciseService.reAnswerExercise(id).subscribe({
      next: (res) => {
        this.actionLoading.set(false);
        this.router.navigate(['/exercise', res.newExerciseId]);
      },
      error: () => this.actionLoading.set(false)
    });
  }

  fixMistakes() {
    const section = this.result()?.sectionCode;
    if (!section) return;
    
    // We can call retakeMistakes scoped to this section. 
    // It will fetch wrong questions from this section (including the ones they just got wrong!)
    this.actionLoading.set(true);
    this.exerciseService.retakeMistakes({ sectionCode: section, questionCount: 10 }).subscribe({
      next: (res) => {
        this.actionLoading.set(false);
        this.router.navigate(['/exercise', res.newExerciseId]);
      },
      error: () => this.actionLoading.set(false)
    });
  }
}
