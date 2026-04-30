import { Component, Input, Output, EventEmitter } from '@angular/core';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { ExerciseResponse } from '../../../../shared/models/api.models';

@Component({
  selector: 'app-cloze-player',
  standalone: true,
  imports: [MatSelectModule, MatButtonModule, MatIconModule, FormsModule],
  template: `
    <div class="cloze-container glass-card animate-fade-in">
      <h3>Read the passage and fill in the blanks:</h3>
      <div class="passage" [innerHTML]="renderPassage()"></div>

      <div class="blanks-section">
        @for (blank of exercise.questions.blanks; track blank.id) {
          <div class="blank-item">
            <span class="blank-label">({{ blank.id }})</span>
            <mat-select [(ngModel)]="answers[blank.id]" placeholder="Choose an answer"
                        class="blank-select">
              @for (opt of blank.options; track $index) {
                <mat-option [value]="$index">
                  {{ getOptionLetter($index) }}. {{ opt }}
                </mat-option>
              }
            </mat-select>
          </div>
        }
      </div>
    </div>

    <button class="accent-btn submit-btn" (click)="onSubmit()"
            [disabled]="!allAnswered()">
      <mat-icon>send</mat-icon>
      Submit ({{ answeredCount() }}/{{ exercise.questions.blanks.length }})
    </button>
  `,
  styles: [`
    .cloze-container h3 {
      font-size: 16px;
      margin-bottom: 16px;
      color: var(--text-secondary);
    }

    .passage {
      font-size: 15px;
      line-height: 2;
      margin-bottom: 24px;
      padding: 16px;
      background: rgba(0,0,0,0.2);
      border-radius: 8px;
    }

    :host ::ng-deep .blank-marker {
      color: var(--accent-primary);
      font-weight: 700;
      padding: 2px 6px;
      background: rgba(124, 58, 237, 0.15);
      border-radius: 4px;
    }

    .blanks-section {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .blank-item {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .blank-label {
      font-weight: 600;
      color: var(--accent-primary);
      min-width: 30px;
    }

    .blank-select { flex: 1; }

    .submit-btn {
      display: flex;
      align-items: center;
      gap: 8px;
      height: 48px;
      width: 100%;
      justify-content: center;
      font-size: 15px;
      margin-top: 24px;
    }
  `]
})
export class ClozePlayerComponent {
  @Input() exercise!: ExerciseResponse;
  @Output() submitAnswers = new EventEmitter<{ [key: string]: any }>();

  answers: { [key: string]: number } = {};

  getOptionLetter(index: number): string {
    return String.fromCharCode(65 + index);
  }

  renderPassage(): string {
    let passage = this.exercise.questions.passage || '';
    passage = passage.replace(/\((\d+)\)___/g, '<span class="blank-marker">($1)___</span>');
    return passage;
  }

  answeredCount(): number {
    return Object.keys(this.answers).length;
  }

  allAnswered(): boolean {
    return this.answeredCount() === (this.exercise.questions.blanks?.length || 0);
  }

  onSubmit() {
    this.submitAnswers.emit({ ...this.answers });
  }
}
