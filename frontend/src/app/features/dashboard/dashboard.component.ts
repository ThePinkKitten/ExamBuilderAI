import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ExerciseService } from '../../core/services/exercise.service';
import { AuthService } from '../../core/services/auth.service';
import { SectionInfo } from '../../shared/models/api.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DecimalPipe, MatCardModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatTooltipModule],
  template: `
    <div class="page-container">
      <div class="page-header animate-fade-in">
        <h1>Hello, <span class="gradient-text">{{ auth.currentUser()?.displayName }}</span> 👋</h1>
        <p>Choose an exercise type to get started</p>
      </div>

      @if (loading()) {
        <div class="loading-center">
          <mat-spinner diameter="48"></mat-spinner>
        </div>
      } @else {
        <div class="sections-grid">
          @for (section of sections(); track section.code; let i = $index) {
            <div class="section-card glass-card" 
                 [class.empty-state]="section.totalExercises === 0"
                 [style.animation-delay]="(i * 0.05) + 's'"
                 (click)="section.totalExercises > 0 ? goToGenerate(section) : null">
              
              <div class="card-content">
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
                        {{ section.totalExercises }} done
                      </span>
                      <span class="stat" [class]="getScoreClass(section.averageScore)" 
                            matTooltip="Average Accuracy" matTooltipPosition="above">
                        <mat-icon>ads_click</mat-icon>
                        {{ section.averageScore | number:'1.0-0' }}%
                      </span>
                    } @else {
                      <span class="stat new-badge">
                        <mat-icon>fiber_new</mat-icon>
                        No exercises yet
                      </span>
                    }
                  </div>
                </div>
              </div>

              <div class="card-actions">
                @if (section.totalExercises === 0) {
                  <button mat-stroked-button color="primary" class="cta-btn" (click)="goToGenerate(section); $event.stopPropagation()">
                    <mat-icon>add</mat-icon>
                    Create Now
                  </button>
                } @else {
                  <mat-icon class="arrow-icon">chevron_right</mat-icon>
                }
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .page-header {
      margin-bottom: 32px;
      padding-left: 48px; // Make room for the floating toggle button
      text-align: left;

      h1 {
        font-size: 28px;
        font-weight: 700;
        margin-bottom: 8px;
        color: var(--text-primary);
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
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
      gap: 20px;
    }

    .section-card {
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      cursor: pointer;
      padding: 24px;
      height: 100%;
      min-height: 180px;
      animation: fadeInUp 0.5s ease-out both;
      position: relative;
      background: white;
      border: 1px solid var(--border);
      border-radius: 16px;
      box-shadow: none !important;

      &:hover {
        transform: translateY(-4px);
        border-color: var(--accent-primary);
        background: var(--bg-card-hover);
        box-shadow: 0 8px 24px rgba(225, 29, 72, 0.08) !important;
        
        .arrow-icon { opacity: 1; transform: translateX(4px); }
      }

      &.empty-state {
        opacity: 0.7;
        cursor: default;
        &:hover { transform: none; border-color: var(--border); background: white; box-shadow: none !important; }
      }
    }

    .card-content {
      display: flex;
      gap: 16px;
      margin-bottom: 16px;
    }

    .section-icon-wrap {
      width: 48px;
      height: 48px;
      border-radius: 14px;
      background: var(--accent-gradient);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      box-shadow: 0 4px 12px rgba(244, 63, 94, 0.2);

      mat-icon {
        color: white;
        font-size: 24px;
      }
    }

    .section-info {
      flex: 1;

      h3 {
        font-size: 17px;
        font-weight: 700;
        margin-bottom: 6px;
        color: var(--text-primary);
      }

      .section-desc {
        color: var(--text-secondary);
        font-size: 13px;
        margin-bottom: 12px;
        line-height: 1.5;
        display: -webkit-box;
        -webkit-line-clamp: 2;
        -webkit-box-orient: vertical;
        overflow: hidden;
      }
    }

    .section-stats {
      display: flex;
      gap: 12px;
    }

    .stat {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 12px;
      font-weight: 500;
      color: var(--text-muted);
      padding: 4px 8px;
      background: rgba(0, 0, 0, 0.03);
      border-radius: 6px;

      mat-icon {
        font-size: 14px;
        width: 14px;
        height: 14px;
      }
    }

    .new-badge {
      color: var(--accent-primary);
      background: rgba(244, 63, 94, 0.08);
    }

    .card-actions {
      display: flex;
      justify-content: flex-end;
      align-items: center;
      margin-top: auto;
    }

    .cta-btn {
      font-size: 12px;
      height: 32px;
      line-height: 32px;
      border-radius: 8px;
      padding: 0 12px;
      
      mat-icon { font-size: 16px; width: 16px; height: 16px; }
    }

    .arrow-icon {
      color: var(--accent-primary);
      opacity: 0.5;
      transition: all 0.2s ease;
    }

    .score-high { color: var(--success) !important; background: rgba(16, 185, 129, 0.08) !important; }
    .score-medium { color: var(--warning) !important; background: rgba(245, 158, 11, 0.08) !important; }
    .score-low { color: var(--danger) !important; background: rgba(239, 68, 68, 0.08) !important; }
  `]
})
export class DashboardComponent implements OnInit {
  sections = signal<SectionInfo[]>([]);
  loading = signal(true);

  constructor(
    public auth: AuthService,
    private exerciseService: ExerciseService,
    private router: Router
  ) { }

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
