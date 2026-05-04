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
            @for (opt of getShuffledOptions(q); track $index) {
              <mat-radio-button [value]="opt.originalIndex" class="option-item">
                <div class="option-content">
                  <span class="option-label">{{ getOptionLetter($index) }}.</span>
                  <span [innerHTML]="formatOption(opt.text, opt.underlinedPart)"></span>
                </div>
              </mat-radio-button>
            }
          </mat-radio-group>
        </div>
      }
    </div>

    <button class="accent-btn submit-btn" (click)="onSubmit()"
            [disabled]="!allAnswered()">
      <mat-icon>send</mat-icon>
      Submit ({{ answeredCount() }}/{{ exercise.questionCount || 0 }})
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
      margin-bottom: 8px;
    }

    .question-number {
      font-size: 12px;
      font-weight: 600;
      color: var(--accent-primary);
      text-transform: uppercase;
      margin-bottom: 8px;
      opacity: 0.8;
    }

    .instruction {
      color: var(--text-secondary);
      font-size: 13px;
      margin-bottom: 8px;
      font-style: italic;
    }

    .sentence {
      font-size: 16px;
      margin-bottom: 16px;
      line-height: 1.6;
      color: #333;
    }

    .options-group {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }

    .option-item {
      margin: 0;
      border-radius: 12px;
      border: 1px solid var(--border);
      transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
      background: white;
      width: 100%;

      &:hover {
        background: rgba(244, 63, 94, 0.03);
        border-color: rgba(244, 63, 94, 0.2);
      }

      &.mat-mdc-radio-button.mat-mdc-radio-checked {
        background: rgba(244, 63, 94, 0.05);
        border-color: var(--accent-primary);
      }
    }

    .option-content {
      display: flex;
      align-items: center;
      width: 100%;
      padding: 10px 0;
    }

    .option-label {
      font-weight: 700;
      color: #333;
      margin-right: 6px;
    }

    // Custom Radio Button Styling
    :host ::ng-deep {
      .mat-mdc-radio-button {
        width: 100%;
        
        .mdc-form-field {
          width: 100%;
          padding: 0 12px;
        }

        .mdc-radio {
          padding: 8px;
          
          .mdc-radio__native-control:enabled:not(:checked) + .mdc-radio__background {
            .mdc-radio__outer-circle { border-color: #cbd5e1; }
          }
          
          .mdc-radio__native-control:enabled:checked + .mdc-radio__background {
            .mdc-radio__outer-circle { border-color: var(--accent-primary); }
            .mdc-radio__inner-circle { border-color: var(--accent-primary); }
          }
        }
        
        .mdc-label {
          display: flex;
          align-items: center;
          width: 100%;
          padding-left: 4px;
          color: #333;
          font-size: 15px;
        }
      }

      .underlined {
        text-decoration: underline;
        font-weight: 600;
        color: var(--accent-secondary);
      }

      .pronunciation-underline {
        text-decoration: underline;
        text-decoration-color: var(--accent-primary);
        text-decoration-thickness: 2px;
        font-weight: 700;
        color: var(--accent-primary);
      }
    }

    .submit-btn {
      display: flex;
      align-items: center;
      gap: 8px;
      height: 52px;
      width: 100%;
      justify-content: center;
      font-size: 16px;
      font-weight: 600;
      border-radius: 14px;
      transition: all 0.2s ease;
      box-shadow: none; // Reset any global shadows

      &:disabled {
        background: #f3f4f6 !important;
        color: #9ca3af !important;
        opacity: 1;
        box-shadow: none !important;
        filter: none !important;
      }
      
      &:not(:disabled) {
        background: var(--accent-gradient);
        color: white;
        box-shadow: 0 4px 15px rgba(244, 63, 94, 0.2);
      }
    }
  `]
})
export class McqPlayerComponent {
  @Input() set exercise(val: ExerciseResponse) {
    this._exercise = val;
    this.initShuffledOptions();
  }
  get exercise(): ExerciseResponse {
    return this._exercise;
  }
  private _exercise!: ExerciseResponse;

  @Output() submitAnswers = new EventEmitter<{ [key: string]: any }>();

  answers: { [key: string]: number } = {};
  
  // Cache for shuffled options: questionId -> array of mapped options
  private shuffledOptionsMap: { [questionId: string]: { text: string, originalIndex: number, underlinedPart?: string }[] } = {};

  initShuffledOptions() {
    if (!this._exercise || !this._exercise.questions || !this._exercise.questions.questions) return;
    
    this.shuffledOptionsMap = {};
    for (const q of this._exercise.questions.questions) {
      if (!q.options) continue;
      
      const mappedOptions = q.options.map((opt: string, idx: number) => ({
        text: opt,
        originalIndex: idx,
        underlinedPart: q.underlinedParts ? q.underlinedParts[idx] : undefined
      }));
      
      // Fisher-Yates shuffle
      for (let i = mappedOptions.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [mappedOptions[i], mappedOptions[j]] = [mappedOptions[j], mappedOptions[i]];
      }
      
      this.shuffledOptionsMap[q.id] = mappedOptions;
    }
  }

  getShuffledOptions(q: any) {
    return this.shuffledOptionsMap[q.id] || [];
  }

  getOptionLetter(index: number): string {
    return String.fromCharCode(65 + index);
  }

  highlightUnderline(sentence: string, word?: string): string {
    if (!word) return sentence;
    return sentence.replace(new RegExp(`\\b${word}\\b`, 'gi'),
      `<span class="underlined">${word}</span>`);
  }

  formatOption(option: string, underlinedPart?: string): string {
    if (!underlinedPart) return option;
    
    const index = option.toLowerCase().indexOf(underlinedPart.toLowerCase());
    if (index === -1) return option; // Fallback if part is not found
    
    const before = option.substring(0, index);
    const match = option.substring(index, index + underlinedPart.length);
    const after = option.substring(index + underlinedPart.length);
    
    return `${before}<u class="pronunciation-underline">${match}</u>${after}`;
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
