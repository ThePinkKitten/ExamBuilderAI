import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { DecimalPipe, DatePipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ExerciseService } from '../../core/services/exercise.service';
import { ExerciseHistoryItem } from '../../shared/models/api.models';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [MatIconModule, MatButtonModule, DecimalPipe, DatePipe, MatProgressSpinnerModule],
  template: `
    <div class="page-container">
      <div class="page-header animate-fade-in">
        <h1 class="gradient-text">History</h1>
        <p>Review and re-answer your past exercises</p>
      </div>

      @if (loading()) {
        <div class="loading-center">
          <mat-spinner diameter="48"></mat-spinner>
        </div>
      } @else if (items().length > 0) {
        <div class="history-list animate-fade-in">
          @for (item of items(); track item.exerciseId; let i = $index) {
            <div class="history-card glass-card" [style.animation-delay]="(i * 0.05) + 's'">
              <div class="history-info">
                <h3>{{ item.sectionName }}</h3>
                <div class="history-meta">
                  <span class="meta-item">
                    <mat-icon>calendar_today</mat-icon>
                    {{ item.createdAt | date:'medium' }}
                  </span>
                  @if (item.unitTitle) {
                    <span class="meta-item">
                      <mat-icon>menu_book</mat-icon>
                      {{ item.unitTitle }}
                    </span>
                  }
                </div>
              </div>
              
              <div class="history-score">
                @if (item.scorePercent !== null) {
                  <span class="score-badge" [class]="getScoreClass(item.scorePercent)">
                    {{ item.scorePercent | number:'1.0-0' }}%
                  </span>
                } @else {
                  <span class="score-badge incomplete">Incomplete</span>
                }
              </div>

              <div class="history-actions">
                @if (item.scorePercent !== null) {
                  <button mat-stroked-button color="primary" (click)="checkAnswers(item.exerciseId)">
                    <mat-icon>visibility</mat-icon> Check
                  </button>
                }
                <button mat-flat-button class="accent-btn" (click)="reAnswer(item.exerciseId)" [disabled]="actionLoading() === item.exerciseId">
                  @if (actionLoading() === item.exerciseId) {
                    <mat-spinner diameter="20"></mat-spinner>
                  } @else {
                    <mat-icon>replay</mat-icon> Re-answer
                  }
                </button>
              </div>
            </div>
          }
        </div>
      } @else {
        <div class="empty-state glass-card">
          <mat-icon>history</mat-icon>
          <h3>No history yet</h3>
          <p>Complete some exercises to see them here.</p>
          <button mat-flat-button class="accent-btn" routerLink="/dashboard" style="margin-top: 16px;">
            Go to Dashboard
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .page-header {
      margin-bottom: 32px;
      h1 { font-size: 28px; font-weight: 700; margin-bottom: 8px; }
      p { color: var(--text-secondary); font-size: 15px; }
    }

    .loading-center {
      display: flex;
      justify-content: center;
      padding: 80px 0;
    }

    .history-list {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .history-card {
      display: flex;
      align-items: center;
      gap: 20px;
      padding: 20px;
      animation: fadeInUp 0.5s ease-out both;

      @media (max-width: 600px) {
        flex-direction: column;
        align-items: flex-start;
      }
    }

    .history-info {
      flex: 1;

      h3 {
        font-size: 16px;
        font-weight: 600;
        margin-bottom: 8px;
      }
    }

    .history-meta {
      display: flex;
      flex-wrap: wrap;
      gap: 16px;
    }

    .meta-item {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 13px;
      color: var(--text-muted);

      mat-icon {
        font-size: 16px;
        width: 16px;
        height: 16px;
      }
    }

    .score-badge {
      padding: 6px 12px;
      border-radius: 20px;
      font-size: 14px;
      font-weight: 600;
      background: rgba(255,255,255,0.1);

      &.score-high { color: var(--success); background: rgba(16, 185, 129, 0.1); }
      &.score-medium { color: var(--warning); background: rgba(245, 158, 11, 0.1); }
      &.score-low { color: var(--danger); background: rgba(239, 68, 68, 0.1); }
      &.incomplete { color: var(--text-muted); }
    }

    .history-actions {
      display: flex;
      gap: 12px;
    }

    .empty-state {
      text-align: center;
      padding: 64px 24px;

      mat-icon {
        font-size: 64px;
        width: 64px;
        height: 64px;
        color: var(--text-muted);
        margin-bottom: 16px;
        opacity: 0.5;
      }

      h3 {
        font-size: 20px;
        margin-bottom: 8px;
      }

      p {
        color: var(--text-secondary);
      }
    }
  `]
})
export class HistoryComponent implements OnInit {
  items = signal<ExerciseHistoryItem[]>([]);
  loading = signal(true);
  actionLoading = signal<number | null>(null);

  constructor(
    private exerciseService: ExerciseService,
    private router: Router
  ) {}

  ngOnInit() {
    this.exerciseService.getHistory(1, 50).subscribe({
      next: (res) => {
        this.items.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  getScoreClass(score: number): string {
    if (score >= 80) return 'score-high';
    if (score >= 50) return 'score-medium';
    return 'score-low';
  }

  checkAnswers(id: number) {
    this.router.navigate(['/exercise', id, 'result']);
  }

  reAnswer(id: number) {
    this.actionLoading.set(id);
    this.exerciseService.reAnswerExercise(id).subscribe({
      next: (res) => {
        this.actionLoading.set(null);
        this.router.navigate(['/exercise', res.newExerciseId]);
      },
      error: () => this.actionLoading.set(null)
    });
  }
}
