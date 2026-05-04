import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ExerciseService } from '../../../core/services/exercise.service';
import { ExerciseResponse } from '../../../shared/models/api.models';
import { McqPlayerComponent } from './mcq-player/mcq-player.component';
import { ClozePlayerComponent } from './cloze-player/cloze-player.component';
import { ReadingPlayerComponent } from './reading-player/reading-player.component';
import { FillInPlayerComponent } from './fill-in-player/fill-in-player.component';
import { WritingPlayerComponent } from './writing-player/writing-player.component';

@Component({
  selector: 'app-exercise-player',
  standalone: true,
  imports: [MatProgressSpinnerModule, MatButtonModule, MatIconModule,
            McqPlayerComponent, ClozePlayerComponent, ReadingPlayerComponent,
            FillInPlayerComponent, WritingPlayerComponent],
  template: `
    <div class="page-container focus-page">
      @if (loading()) {
        <div class="loading-center">
          <mat-spinner diameter="48"></mat-spinner>
          <p>Loading exercise...</p>
        </div>
      } @else if (exercise()) {
        <div class="player-master-wrapper animate-fade-in">
          <header class="player-header">
            <div class="header-side">
              <button mat-icon-button (click)="goBack()" class="back-btn-player" matTooltip="Back to Dashboard">
                <mat-icon>arrow_back</mat-icon>
              </button>
            </div>
            
            <div class="header-center">
              <h2 class="gradient-text">{{ exercise()!.sectionName }}</h2>
              <p class="exercise-meta">
                @if (exercise()!.unitTitle) {
                  <span>{{ exercise()!.unitTitle }}</span> ·
                }
                <span>{{ exercise()!.questionCount }} {{ exercise()!.questionCount === 1 ? 'question' : 'questions' }}</span>
              </p>
            </div>

            <div class="header-side align-right">
              <div class="timer-card">
                <mat-icon>timer</mat-icon>
                <span>{{ formatTime(elapsedSeconds()) }}</span>
              </div>
            </div>
          </header>

          <main class="player-content">
            @switch (getPlayerType(exercise()!.sectionCode)) {
              @case ('mcq') {
                <app-mcq-player
                  [exercise]="exercise()!"
                  (submitAnswers)="onSubmit($event)" />
              }
              @case ('cloze') {
                <app-cloze-player
                  [exercise]="exercise()!"
                  (submitAnswers)="onSubmit($event)" />
              }
              @case ('reading') {
                <app-reading-player
                  [exercise]="exercise()!"
                  (submitAnswers)="onSubmit($event)" />
              }
              @case ('fill-in') {
                <app-fill-in-player
                  [exercise]="exercise()!"
                  (submitAnswers)="onSubmit($event)" />
              }
              @case ('writing') {
                <app-writing-player
                  [exercise]="exercise()!"
                  (submitAnswers)="onSubmit($event)" />
              }
            }
          </main>
        </div>
      }

      @if (submitting()) {
        <div class="submit-overlay">
          <mat-spinner diameter="40"></mat-spinner>
          <p>Grading...</p>
        </div>
      }
    </div>
  `,
  styles: [`
    .loading-center {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 16px;
      padding: 80px 0;
      color: var(--text-secondary);
    }

    .focus-page {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding-top: 32px !important;
    }

    .focus-page {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 32px 24px !important;
    }

    .player-master-wrapper {
      width: 100%;
      max-width: 800px;
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .player-header {
      width: 100%;
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .header-side {
      flex: 1;
      display: flex;
      align-items: center;
      
      &.align-right {
        justify-content: flex-end;
      }
    }

    .header-center {
      flex: 2;
      text-align: center;
      
      h2 { 
        font-size: 28px; 
        font-weight: 700;
        margin-bottom: 4px;
        line-height: 1.2;
      }

      .exercise-meta {
        color: var(--text-secondary);
        font-size: 14px;
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 6px;
      }
    }

    .back-btn-player {
      color: var(--text-muted);
      margin-left: -12px;
    }

    .timer-card {
      display: flex;
      align-items: center;
      gap: 8px;
      color: var(--accent-primary);
      font-size: 18px;
      font-weight: 700;
      font-variant-numeric: tabular-nums;
      background: rgba(244, 63, 94, 0.05);
      padding: 6px 16px;
      border-radius: 20px;
    }

    .player-content {
      width: 100%;
    }

    .submit-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.6);
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 16px;
      z-index: 100;
      color: white;
      font-size: 16px;
    }
  `]
})
export class ExercisePlayerComponent implements OnInit {
  exercise = signal<ExerciseResponse | null>(null);
  loading = signal(true);
  submitting = signal(false);
  elapsedSeconds = signal(0);
  private timerInterval: any;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private exerciseService: ExerciseService
  ) {}

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.exerciseService.getExercise(id).subscribe({
      next: (ex) => {
        this.exercise.set(ex);
        this.loading.set(false);
        this.startTimer();
      },
      error: () => this.loading.set(false)
    });
  }

  getPlayerType(code: string): string {
    if (['pronunciation', 'stress', 'grammar_vocab', 'synonym', 'antonym'].includes(code)) return 'mcq';
    if (code === 'cloze_test') return 'cloze';
    if (code === 'reading') return 'reading';
    if (['sentence_completion', 'word_form'].includes(code)) return 'fill-in';
    if (code === 'paragraph_writing') return 'writing';
    return 'mcq';
  }

  onSubmit(answers: { [key: string]: any }) {
    this.submitting.set(true);
    this.stopTimer();

    const ex = this.exercise()!;
    this.exerciseService.submit(ex.id, {
      userAnswers: answers,
      timeTakenSeconds: this.elapsedSeconds()
    }).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.router.navigate(['/exercise', ex.id, 'result'], {
          state: { result }
        });
      },
      error: () => {
        this.submitting.set(false);
      }
    });
  }

  formatTime(seconds: number): string {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  }

  goBack() {
    this.stopTimer();
    this.router.navigate(['/dashboard']);
  }

  private startTimer() {
    this.timerInterval = setInterval(() => {
      this.elapsedSeconds.update(v => v + 1);
    }, 1000);
  }

  private stopTimer() {
    if (this.timerInterval) clearInterval(this.timerInterval);
  }

  ngOnDestroy() {
    this.stopTimer();
  }
}
