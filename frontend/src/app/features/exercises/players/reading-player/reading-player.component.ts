import { Component, Input, Output, EventEmitter } from '@angular/core';
import { MatRadioModule } from '@angular/material/radio';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { ExerciseResponse } from '../../../../shared/models/api.models';

@Component({
  selector: 'app-reading-player',
  standalone: true,
  imports: [MatRadioModule, MatButtonModule, MatIconModule, FormsModule],
  template: `
    <div class="reading-layout">
      <div class="passage-panel glass-card animate-fade-in">
        <h3><mat-icon>menu_book</mat-icon> Passage</h3>
        <div class="passage-text">{{ exercise.questions.passage }}</div>
      </div>

      <div class="questions-panel">
        @for (q of exercise.questions.questions; track q.id; let i = $index) {
          <div class="question-card glass-card animate-slide-in"
               [style.animation-delay]="(i * 0.1) + 's'">
            <div class="question-number">Question {{ q.id }}</div>

            @if (q.type === 'true_false') {
              <p class="question-text">{{ q.statement }}</p>
              <mat-radio-group [(ngModel)]="answers[q.id]" class="tf-group">
                <mat-radio-button [value]="true" class="option-item">True</mat-radio-button>
                <mat-radio-button [value]="false" class="option-item">False</mat-radio-button>
              </mat-radio-group>
            } @else {
              <p class="question-text">{{ q.question }}</p>
              <mat-radio-group [(ngModel)]="answers[q.id]" class="options-group">
                @for (opt of q.options; track $index) {
                  <mat-radio-button [value]="$index" class="option-item">
                    {{ opt }}
                  </mat-radio-button>
                }
              </mat-radio-group>
            }
          </div>
        }

        <button class="accent-btn submit-btn" (click)="onSubmit()"
                [disabled]="!allAnswered()">
          <mat-icon>send</mat-icon>
          Submit ({{ answeredCount() }}/{{ exercise.questions.questions.length }})
        </button>
      </div>
    </div>
  `,
  styles: [`
    .reading-layout {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 20px;
      align-items: start;
    }

    .passage-panel {
      position: sticky;
      top: 24px;

      h3 {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 16px;
        margin-bottom: 16px;
        color: var(--accent-secondary);
      }
    }

    .passage-text {
      font-size: 14px;
      line-height: 1.8;
      color: var(--text-secondary);
    }

    .questions-panel {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .question-card { animation: slideInRight 0.4s ease-out both; }
    .question-number {
      font-size: 12px;
      font-weight: 600;
      color: var(--accent-primary);
      margin-bottom: 8px;
    }

    .question-text {
      font-size: 15px;
      margin-bottom: 12px;
      line-height: 1.5;
    }

    .options-group, .tf-group {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .tf-group { flex-direction: row; gap: 16px; }

    .option-item {
      padding: 8px 12px;
      border-radius: 8px;
      border: 1px solid var(--border);
      transition: all 0.2s;
      &:hover { border-color: var(--accent-primary); }
    }

    .submit-btn {
      display: flex;
      align-items: center;
      gap: 8px;
      height: 48px;
      width: 100%;
      justify-content: center;
    }

    @media (max-width: 768px) {
      .reading-layout { grid-template-columns: 1fr; }
      .passage-panel { position: static; }
    }
  `]
})
export class ReadingPlayerComponent {
  @Input() exercise!: ExerciseResponse;
  @Output() submitAnswers = new EventEmitter<{ [key: string]: any }>();

  answers: { [key: string]: any } = {};

  answeredCount(): number {
    return Object.keys(this.answers).length;
  }

  allAnswered(): boolean {
    return this.answeredCount() === (this.exercise.questions.questions?.length || 0);
  }

  onSubmit() {
    this.submitAnswers.emit({ ...this.answers });
  }
}
