import { Component, OnInit, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DecimalPipe } from '@angular/common';
import { ProgressService } from '../../core/services/progress.service';
import { ProgressOverview } from '../../shared/models/api.models';

@Component({
  selector: 'app-progress',
  standalone: true,
  imports: [MatIconModule, MatProgressSpinnerModule, DecimalPipe],
  template: `
    <div class="page-container">
      <div class="page-header animate-fade-in">
        <h1 class="gradient-text">Progress</h1>
        <p>Track your learning progress</p>
      </div>

      @if (loading()) {
        <div class="loading-center">
          <mat-spinner diameter="48"></mat-spinner>
        </div>
      } @else if (overview()) {
        <!-- Stats Cards -->
        <div class="stats-grid animate-fade-in">
          <div class="stat-card">
            <mat-icon>assignment_turned_in</mat-icon>
            <div class="stat-value">{{ overview()!.totalExercisesDone }}</div>
            <div class="stat-label">Completed exercises</div>
          </div>
          <div class="stat-card">
            <mat-icon>star</mat-icon>
            <div class="stat-value">{{ overview()!.overallAverageScore | number:'1.0-0' }}%</div>
            <div class="stat-label">Average score</div>
          </div>
          <div class="stat-card">
            <mat-icon>check_circle</mat-icon>
            <div class="stat-value">{{ overview()!.totalCorrectAnswers }}/{{ overview()!.totalQuestionsAnswered }}</div>
            <div class="stat-label">Correct answers</div>
          </div>
        </div>

        <!-- Section Breakdown -->
        @if (overview()!.sectionStats.length > 0) {
          <div class="section-card animate-fade-in">
            <h3 class="section-card-header">📊 Scores by section</h3>
            <div class="section-bars">
              @for (stat of overview()!.sectionStats; track stat.sectionCode) {
                <div class="bar-item">
                  <div class="bar-header">
                    <span class="bar-name">{{ stat.sectionName }}</span>
                    <span class="bar-score" [class]="getScoreClass(stat.averageScore)">
                      {{ stat.averageScore | number:'1.0-0' }}%
                    </span>
                  </div>
                  <div class="bar-track">
                    <div class="bar-fill" [class]="getScoreClass(stat.averageScore)"
                         [style.width.%]="stat.averageScore"></div>
                  </div>
                  <div class="bar-detail">
                    {{ stat.exercisesDone }} exercise{{ stat.exercisesDone !== 1 ? 's' : '' }} · {{ stat.correctAnswers }}/{{ stat.totalQuestions }} correct
                  </div>
                </div>
              }
            </div>
          </div>
        } @else {
          <div class="empty-state">
            <mat-icon>school</mat-icon>
            <h3>No data yet</h3>
            <p>Complete your first exercise to see your progress!</p>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .page-header {
      margin-bottom: 32px;
      padding-left: 48px; // Room for toggle button

      h1 { font-size: 28px; font-weight: 700; }
      p { color: var(--text-secondary); font-size: 15px; }
    }

    .loading-center {
      display: flex;
      justify-content: center;
      padding: 80px 0;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 16px;
      margin-bottom: 32px;
    }

    .stat-card {
      text-align: center;
      padding: 24px;
      background: white;
      border: 1px solid var(--border);
      border-radius: 16px;
      box-shadow: none !important; // Explicitly remove any global shadows

      mat-icon {
        font-size: 32px;
        width: 32px;
        height: 32px;
        color: var(--accent-primary);
        margin-bottom: 12px;
      }

      .stat-value {
        font-size: 28px;
        font-weight: 700;
        margin-bottom: 4px;
      }

      .stat-label {
        color: var(--text-secondary);
        font-size: 13px;
        font-weight: 500;
      }
    }

    .section-card {
      background: white;
      border: 1px solid var(--border);
      border-radius: 16px;
      padding: 24px;
      width: 100%; // Full width to align with stats
      box-shadow: none !important;
    }

    .section-card-header {
      font-size: 18px;
      font-weight: 700;
      color: #333;
      padding-bottom: 16px;
      margin-bottom: 24px;
      border-bottom: 1px solid #f1f5f9;
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .section-bars {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .bar-item {
      .bar-header {
        display: flex;
        justify-content: space-between;
        margin-bottom: 8px;
      }

      .bar-name {
        font-size: 14px;
        font-weight: 600;
        color: #333;
      }

      .bar-score {
        font-size: 14px;
        font-weight: 700;
      }
    }

    .bar-track {
      height: 8px;
      background: #f1f5f9; // Consistent light-gray track
      border-radius: 10px;
      overflow: hidden;
      width: 100%;
    }

    .bar-fill {
      height: 100%;
      border-radius: 10px;
      transition: width 1s cubic-bezier(0.4, 0, 0.2, 1);

      &.score-high { background: var(--success); }
      &.score-medium { background: var(--warning); }
      &.score-low { background: var(--danger); }
    }

    .bar-detail {
      font-size: 12px;
      color: var(--text-muted);
      margin-top: 6px;
      font-weight: 500;
    }

    .score-high { color: var(--success); }
    .score-medium { color: var(--warning); }
    .score-low { color: var(--danger); }

    .empty-state {
      text-align: center;
      padding: 60px 24px;
      background: white;
      border: 1px solid var(--border);
      border-radius: 16px;

      mat-icon {
        font-size: 48px;
        width: 48px;
        height: 48px;
        color: var(--text-muted);
        margin-bottom: 16px;
      }

      h3 { margin-bottom: 8px; font-weight: 700; }
      p { color: var(--text-secondary); }
    }
  `]
})
export class ProgressComponent implements OnInit {
  overview = signal<ProgressOverview | null>(null);
  loading = signal(true);

  constructor(private progressService: ProgressService) {}

  ngOnInit() {
    this.progressService.getOverview().subscribe({
      next: (data) => {
        this.overview.set(data);
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
}
