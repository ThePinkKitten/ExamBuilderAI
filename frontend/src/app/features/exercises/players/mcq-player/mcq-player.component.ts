import { Component, Input, Output, EventEmitter } from '@angular/core';
import { MatRadioModule } from '@angular/material/radio';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { ExerciseResponse } from '../../../../shared/models/api.models';

@Component({
  selector: 'app-mcq-player',
  standalone: true,
  imports: [MatRadioModule, MatButtonModule, MatIconModule, FormsModule],
  template: `
    <div class="questions-list">
      @for (q of exercise.questions.questions; track q.id; let i = $index) {
        <div class="question-card glass-card" [style.animation-delay]="(i * 0.1) + 's'">
          <div class="question-number">Question {{ q.id }}</div>

          @if (q.instruction) {
            <p class="instruction">{{ q.instruction }}</p>
          }

          @if (q.sentence) {
            <p class="sentence" [innerHTML]="highlightUnderline(q.sentence, q.underlinedWord)"></p>
          }

          <mat-radio-group [(ngModel)]="answers[q.id]" class="options-group">
            @for (opt of q.options; track $index) {
              <mat-radio-button [value]="$index" class="option-item">
                <span class="option-label">{{ getOptionLetter($index) }}.</span>
                <span>{{ opt }}</span>
                @if (q.underlinedParts && q.underlinedParts[$index]) {
                  <span class="phonetic-hint">/{{ q.underlinedParts[$index] }}/</span>
                }
              </mat-radio-button>
            }
          </mat-radio-group>
        </div>
      }
    </div>

    <button class="accent-btn submit-btn" (click)="onSubmit()"
            [disabled]="!allAnswered()">
      <mat-icon>send</mat-icon>
      Submit ({{ answeredCount() }}/{{ exercise.questionCount }})
    </button>
  `,
  styles: [`
    .questions-list {
      display: flex;
      flex-direction: column;
      gap: 16px;
      margin-bottom: 24px;
    }

    .question-card {
      animation: fadeInUp 0.5s ease-out both;
    }

    .question-number {
      font-size: 12px;
      font-weight: 600;
      color: var(--accent-primary);
      text-transform: uppercase;
      margin-bottom: 8px;
    }

    .instruction {
      color: var(--text-secondary);
      font-size: 13px;
      margin-bottom: 8px;
      font-style: italic;
    }

    .sentence {
      font-size: 15px;
      margin-bottom: 12px;
      line-height: 1.6;
    }

    .options-group {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .option-item {
      padding: 8px 12px;
      border-radius: 8px;
      border: 1px solid var(--border);
      transition: all 0.2s ease;

      &:hover {
        border-color: var(--accent-primary);
        background: rgba(124, 58, 237, 0.05);
      }
    }

    .option-label {
      font-weight: 600;
      color: var(--accent-primary);
      margin-right: 4px;
    }

    .phonetic-hint {
      color: var(--text-muted);
      font-size: 12px;
      margin-left: 8px;
    }

    :host ::ng-deep .underlined {
      text-decoration: underline;
      font-weight: 600;
      color: var(--accent-secondary);
    }

    .submit-btn {
      display: flex;
      align-items: center;
      gap: 8px;
      height: 48px;
      width: 100%;
      justify-content: center;
      font-size: 15px;
    }
  `]
})
export class McqPlayerComponent {
  @Input() exercise!: ExerciseResponse;
  @Output() submitAnswers = new EventEmitter<{ [key: string]: any }>();

  answers: { [key: string]: number } = {};

  getOptionLetter(index: number): string {
    return String.fromCharCode(65 + index);
  }

  highlightUnderline(sentence: string, word?: string): string {
    if (!word) return sentence;
    return sentence.replace(new RegExp(`\\b${word}\\b`, 'gi'),
      `<span class="underlined">${word}</span>`);
  }

  answeredCount(): number {
    return Object.keys(this.answers).length;
  }

  allAnswered(): boolean {
    return this.answeredCount() === this.exercise.questionCount;
  }

  onSubmit() {
    const mapped: { [key: string]: any } = {};
    for (const [k, v] of Object.entries(this.answers)) {
      mapped[k] = v;
    }
    this.submitAnswers.emit(mapped);
  }
}
