import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { ExerciseService } from '../../core/services/exercise.service';
import { SectionInfo } from '../../shared/models/api.models';

@Component({
  selector: 'app-retake',
  standalone: true,
  imports: [
    MatIconModule, MatButtonModule, MatProgressSpinnerModule, 
    MatSelectModule, MatFormFieldModule, FormsModule
  ],
  template: `
    <div class="page-container">
      <div class="page-header animate-fade-in">
        <h1 class="gradient-text">Mistake Bank</h1>
        <p>Review and practice the questions you've gotten wrong in the past.</p>
      </div>

      <div class="retake-container glass-card animate-fade-in">
        <div class="hero-icon">
          <mat-icon>build_circle</mat-icon>
        </div>
        <h2>Fix your mistakes</h2>
        <p class="description">
          We will generate a specialized mini-exercise containing up to 10 questions that you answered incorrectly in your past exercises.
        </p>

        <div class="controls">
          <mat-form-field appearance="outline" class="section-select">
            <mat-label>Filter by section (Optional)</mat-label>
            <mat-select [(ngModel)]="selectedSection">
              <mat-option [value]="null">-- All Sections --</mat-option>
              @for (sec of sections(); track sec.code) {
                <mat-option [value]="sec.code">{{ sec.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        </div>

        @if (errorMessage()) {
          <div class="error-msg">
            <mat-icon>info</mat-icon> {{ errorMessage() }}
          </div>
        }

        <button mat-flat-button class="accent-btn start-btn" 
                (click)="startRetake()" [disabled]="loading()">
          @if (loading()) {
            <mat-spinner diameter="24"></mat-spinner>
          } @else {
            <mat-icon>play_arrow</mat-icon> Practice Mistakes
          }
        </button>
      </div>
    </div>
  `,
  styles: [`
    .page-header {
      margin-bottom: 32px;
      h1 { font-size: 28px; font-weight: 700; margin-bottom: 8px; }
      p { color: var(--text-secondary); font-size: 15px; }
    }

    .retake-container {
      max-width: 600px;
      margin: 0 auto;
      padding: 48px 32px;
      text-align: center;
      display: flex;
      flex-direction: column;
      align-items: center;

      h2 {
        font-size: 24px;
        margin-bottom: 16px;
      }

      .description {
        color: var(--text-secondary);
        line-height: 1.6;
        margin-bottom: 32px;
        max-width: 400px;
      }
    }

    .hero-icon {
      width: 80px;
      height: 80px;
      border-radius: 50%;
      background: rgba(124, 58, 237, 0.1);
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 24px;

      mat-icon {
        font-size: 40px;
        width: 40px;
        height: 40px;
        color: var(--accent-primary);
      }
    }

    .controls {
      width: 100%;
      max-width: 300px;
      margin-bottom: 24px;
    }

    .section-select {
      width: 100%;
    }

    .start-btn {
      padding: 0 32px;
      height: 48px;
      font-size: 16px;
      border-radius: 24px;

      mat-icon {
        margin-right: 8px;
      }
    }

    .error-msg {
      display: flex;
      align-items: center;
      gap: 8px;
      color: var(--warning);
      background: rgba(245, 158, 11, 0.1);
      padding: 12px 24px;
      border-radius: 8px;
      margin-bottom: 24px;
      font-size: 14px;

      mat-icon {
        font-size: 20px;
        width: 20px;
        height: 20px;
      }
    }
  `]
})
export class RetakeComponent implements OnInit {
  sections = signal<SectionInfo[]>([]);
  selectedSection: string | null = null;
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(
    private exerciseService: ExerciseService,
    private router: Router
  ) {}

  ngOnInit() {
    this.exerciseService.getSections().subscribe(res => {
      // Filter out writing because it doesn't support 'wrong questions' concept easily
      this.sections.set(res.filter(s => s.code !== 'paragraph_writing'));
    });
  }

  startRetake() {
    this.loading.set(true);
    this.errorMessage.set(null);
    
    this.exerciseService.retakeMistakes({ 
      sectionCode: this.selectedSection || undefined,
      questionCount: 10
    }).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.router.navigate(['/exercise', res.newExerciseId]);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to generate mistake exercise. Maybe you have no mistakes?');
      }
    });
  }
}
