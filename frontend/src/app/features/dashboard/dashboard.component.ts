import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ExerciseService } from '../../core/services/exercise.service';
import { AuthService } from '../../core/services/auth.service';
import { SectionInfo } from '../../shared/models/api.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DecimalPipe, MatCardModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  template: `
    <div class="page-container">
      <div class="page-header animate-fade-in">
        <h1>Xin chào, <span class="gradient-text">{{ auth.currentUser()?.displayName }}</span> 👋</h1>
        <p>Chọn một dạng bài tập để bắt đầu ôn tập</p>
      </div>

      @if (loading()) {
        <div class="loading-center">
          <mat-spinner diameter="48"></mat-spinner>
        </div>
      } @else {
        <div class="sections-grid">
          @for (section of sections(); track section.code; let i = $index) {
            <div class="section-card glass-card" (click)="goToGenerate(section)"
                 [style.animation-delay]="(i * 0.05) + 's'">
              <div class="section-icon-wrap">
                <mat-icon>{{ section.icon }}</mat-icon>
              </div>
              <div class="section-info">
                <h3>{{ section.name }}</h3>
                <p class="section-desc">{{ section.description }}</p>
                <div class="section-stats">
                  @if (section.totalExercises > 0) {
                    <span class="stat">
                      <mat-icon>check_circle</mat-icon>
                      {{ section.totalExercises }} bài đã làm
                    </span>
                    <span class="stat" [class]="getScoreClass(section.averageScore)">
                      <mat-icon>star</mat-icon>
                      {{ section.averageScore | number:'1.0-0' }}%
                    </span>
                  } @else {
                    <span class="stat new-badge">
                      <mat-icon>fiber_new</mat-icon>
                      Chưa làm bài nào
                    </span>
                  }
                </div>
              </div>
              <mat-icon class="arrow-icon">chevron_right</mat-icon>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .page-header {
      margin-bottom: 32px;

      h1 {
        font-size: 28px;
        font-weight: 700;
        margin-bottom: 8px;
      }

      p {
        color: var(--text-secondary);
        font-size: 15px;
      }
    }

    .loading-center {
      display: flex;
      justify-content: center;
      padding: 80px 0;
    }

    .sections-grid {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .section-card {
      display: flex;
      align-items: center;
      gap: 16px;
      cursor: pointer;
      padding: 20px;
      animation: fadeInUp 0.5s ease-out both;

      &:hover {
        transform: translateX(4px);
        .arrow-icon { opacity: 1; transform: translateX(0); }
      }
    }

    .section-icon-wrap {
      width: 48px;
      height: 48px;
      border-radius: 12px;
      background: var(--accent-gradient);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;

      mat-icon {
        color: white;
        font-size: 24px;
      }
    }

    .section-info {
      flex: 1;

      h3 {
        font-size: 16px;
        font-weight: 600;
        margin-bottom: 4px;
      }

      .section-desc {
        color: var(--text-secondary);
        font-size: 13px;
        margin-bottom: 8px;
        line-height: 1.4;
      }
    }

    .section-stats {
      display: flex;
      gap: 16px;
    }

    .stat {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 12px;
      color: var(--text-muted);

      mat-icon {
        font-size: 14px;
        width: 14px;
        height: 14px;
      }
    }

    .new-badge {
      color: var(--accent-secondary);
    }

    .arrow-icon {
      color: var(--text-muted);
      opacity: 0;
      transform: translateX(-8px);
      transition: all 0.2s ease;
    }

    .score-high { color: var(--success) !important; }
    .score-medium { color: var(--warning) !important; }
    .score-low { color: var(--danger) !important; }
  `]
})
export class DashboardComponent implements OnInit {
  sections = signal<SectionInfo[]>([]);
  loading = signal(true);

  constructor(
    public auth: AuthService,
    private exerciseService: ExerciseService,
    private router: Router
  ) {}

  ngOnInit() {
    this.exerciseService.getSections().subscribe({
      next: (sections) => {
        this.sections.set(sections);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  goToGenerate(section: SectionInfo) {
    this.router.navigate(['/exercise/generate'], {
      queryParams: { section: section.code }
    });
  }

  getScoreClass(score: number | null): string {
    if (score === null) return '';
    if (score >= 80) return 'score-high';
    if (score >= 50) return 'score-medium';
    return 'score-low';
  }
}
