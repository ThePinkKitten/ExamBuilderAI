import { Component, Input, Output, EventEmitter } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { ExerciseResponse } from '../../../../shared/models/api.models';

@Component({
  selector: 'app-fill-in-player',
  standalone: true,
  imports: [MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, FormsModule],
  template: `
    <div class="questions-list">
      @for (q of exercise.questions.questions; track q.id; let i = $index) {
        <div class="question-card glass-card" [style.animation-delay]="(i * 0.1) + 's'">
          <div class="question-number">Câu {{ q.id }}</div>

          @if (q.instruction) {
            <p class="instruction">{{ q.instruction }}</p>
          }

          <p class="prompt">
            @if (q.givenWord) {
              {{ q.sentence }}
            } @else {
              {{ q.prompt }}
            }
          </p>

          <mat-form-field appearance="outline" class="answer-field">
            <mat-label>Nhập đáp án</mat-label>
            <input matInput [(ngModel)]="answers[q.id]" [name]="'q' + q.id"
                   placeholder="Nhập câu trả lời...">
          </mat-form-field>
        </div>
      }
    </div>

    <button class="accent-btn submit-btn" (click)="onSubmit()"
            [disabled]="!allAnswered()">
      <mat-icon>send</mat-icon>
      Nộp bài ({{ answeredCount() }}/{{ exercise.questionCount }})
    </button>
  `,
  styles: [`
    .questions-list {
      display: flex;
      flex-direction: column;
      gap: 16px;
      margin-bottom: 24px;
    }

    .question-card { animation: fadeInUp 0.5s ease-out both; }

    .question-number {
      font-size: 12px;
      font-weight: 600;
      color: var(--accent-primary);
      margin-bottom: 8px;
    }

    .instruction {
      color: var(--text-secondary);
      font-size: 13px;
      margin-bottom: 8px;
      font-style: italic;
    }

    .prompt {
      font-size: 15px;
      line-height: 1.6;
      margin-bottom: 12px;
    }

    .answer-field { width: 100%; }

    .submit-btn {
      display: flex;
      align-items: center;
      gap: 8px;
      height: 48px;
      width: 100%;
      justify-content: center;
    }
  `]
})
export class FillInPlayerComponent {
  @Input() exercise!: ExerciseResponse;
  @Output() submitAnswers = new EventEmitter<{ [key: string]: any }>();

  answers: { [key: string]: string } = {};

  answeredCount(): number {
    return Object.values(this.answers).filter(v => v && v.trim().length > 0).length;
  }

  allAnswered(): boolean {
    return this.answeredCount() === this.exercise.questionCount;
  }

  onSubmit() {
    this.submitAnswers.emit({ ...this.answers });
  }
}
