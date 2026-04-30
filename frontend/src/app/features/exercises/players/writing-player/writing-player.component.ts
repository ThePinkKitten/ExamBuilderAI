import { Component, Input, Output, EventEmitter } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { FormsModule } from '@angular/forms';
import { ExerciseResponse } from '../../../../shared/models/api.models';

@Component({
  selector: 'app-writing-player',
  standalone: true,
  imports: [MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatChipsModule, FormsModule],
  template: `
    <div class="writing-container glass-card animate-fade-in">
      <div class="topic-section">
        <h3><mat-icon>edit</mat-icon> Đề bài</h3>
        <p class="topic-text">{{ exercise.questions.topic }}</p>
      </div>

      @if (exercise.questions.hints?.length) {
        <div class="hints-section">
          <h4>💡 Gợi ý từ vựng:</h4>
          <div class="hints-chips">
            @for (hint of exercise.questions.hints; track hint) {
              <mat-chip>{{ hint }}</mat-chip>
            }
          </div>
        </div>
      }

      @if (exercise.questions.wordCount) {
        <p class="word-count-hint">
          Yêu cầu: {{ exercise.questions.wordCount.min }}-{{ exercise.questions.wordCount.max }} từ
        </p>
      }

      <mat-form-field appearance="outline" class="writing-field">
        <mat-label>Viết đoạn văn của bạn</mat-label>
        <textarea matInput [(ngModel)]="paragraphText" name="paragraph"
                  rows="10" placeholder="Start writing here..."></textarea>
      </mat-form-field>

      <div class="writing-stats">
        <span [class.warning]="wordCount < 60" [class.ok]="wordCount >= 80">
          📝 {{ wordCount }} từ
        </span>
      </div>
    </div>

    <button class="accent-btn submit-btn" (click)="onSubmit()"
            [disabled]="wordCount < 20">
      <mat-icon>send</mat-icon>
      Nộp bài (AI sẽ chấm)
    </button>
  `,
  styles: [`
    .topic-section {
      margin-bottom: 20px;

      h3 {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 16px;
        color: var(--accent-secondary);
        margin-bottom: 12px;
      }

      .topic-text {
        font-size: 16px;
        line-height: 1.6;
        font-weight: 500;
      }
    }

    .hints-section {
      margin-bottom: 16px;

      h4 {
        font-size: 14px;
        color: var(--text-secondary);
        margin-bottom: 8px;
      }
    }

    .hints-chips {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
    }

    .word-count-hint {
      color: var(--text-muted);
      font-size: 13px;
      margin-bottom: 16px;
    }

    .writing-field { width: 100%; }

    .writing-stats {
      text-align: right;
      font-size: 14px;
      margin-top: -8px;
      margin-bottom: 8px;

      .warning { color: var(--warning); }
      .ok { color: var(--success); }
    }

    .submit-btn {
      display: flex;
      align-items: center;
      gap: 8px;
      height: 48px;
      width: 100%;
      justify-content: center;
      margin-top: 16px;
    }
  `]
})
export class WritingPlayerComponent {
  @Input() exercise!: ExerciseResponse;
  @Output() submitAnswers = new EventEmitter<{ [key: string]: any }>();

  paragraphText = '';

  get wordCount(): number {
    return this.paragraphText.trim()
      ? this.paragraphText.trim().split(/\s+/).length
      : 0;
  }

  onSubmit() {
    this.submitAnswers.emit({ '1': this.paragraphText });
  }
}
