import { Component, OnInit, signal } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSliderModule } from '@angular/material/slider';
import { ExerciseService } from '../../../core/services/exercise.service';
import { SectionInfo, CurriculumUnitInfo } from '../../../shared/models/api.models';

@Component({
  selector: 'app-generator',
  standalone: true,
  imports: [FormsModule, MatFormFieldModule, MatSelectModule, MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatSliderModule],
  template: `
    <div class="page-container">
      <div class="page-header animate-fade-in">
        <button mat-icon-button (click)="goBack()">
          <mat-icon>arrow_back</mat-icon>
        </button>
        <div>
          <h1 class="gradient-text">Generate Exercise</h1>
          <p>Choose settings and let AI generate an exercise for you</p>
        </div>
      </div>

      <div class="generator-form glass-card animate-fade-in">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Exercise type</mat-label>
          <mat-select [(ngModel)]="selectedSection" name="section">
            @for (s of sections(); track s.code) {
              <mat-option [value]="s.code">
                {{ s.name }} — {{ s.description }}
              </mat-option>
            }
          </mat-select>
          <mat-icon matPrefix>category</mat-icon>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Unit (optional)</mat-label>
          <mat-select [(ngModel)]="selectedUnit" name="unit">
            <mat-option [value]="null">All units (random)</mat-option>
            @for (u of units(); track u.id) {
              <mat-option [value]="u.id">
                Unit {{ u.unitNumber }}: {{ u.unitTitle }}
              </mat-option>
            }
          </mat-select>
          <mat-icon matPrefix>book</mat-icon>
        </mat-form-field>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Number of questions</mat-label>
            <mat-select [(ngModel)]="questionCount" name="count">
              <mat-option [value]="1">1 question</mat-option>
              <mat-option [value]="3">3 questions</mat-option>
              <mat-option [value]="5">5 questions</mat-option>
              <mat-option [value]="8">8 questions</mat-option>
              <mat-option [value]="10">10 questions</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Difficulty</mat-label>
            <mat-select [(ngModel)]="difficulty" name="difficulty">
              <mat-option value="easy">🟢 Easy</mat-option>
              <mat-option value="medium">🟡 Medium</mat-option>
              <mat-option value="hard">🔴 Hard</mat-option>
            </mat-select>
          </mat-form-field>
        </div>

        <button class="accent-btn generate-btn" (click)="generate()"
                [disabled]="generating() || !selectedSection">
          @if (generating()) {
            <mat-spinner diameter="20"></mat-spinner>
            <span>AI is thinking (about 10-15s)...</span>
          } @else {
            <mat-icon>auto_awesome</mat-icon>
            <span>Generate Exercise</span>
          }
        </button>

        @if (error()) {
          <div class="error-message">
            <mat-icon>error</mat-icon>
            {{ error() }}
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .page-header {
      display: flex;
      align-items: center;
      gap: 16px;
      margin-bottom: 32px;

      h1 { font-size: 24px; font-weight: 700; }
      p { color: var(--text-secondary); font-size: 14px; }
    }

    .generator-form {
      max-width: 600px;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .full-width { width: 100%; }

    .form-row {
      display: flex;
      gap: 16px;

      mat-form-field { flex: 1; }
    }

    .generate-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 10px;
      height: 52px;
      font-size: 16px;
      margin-top: 8px;
    }

    .error-message {
      display: flex;
      align-items: center;
      gap: 8px;
      color: var(--danger);
      font-size: 14px;
      padding: 12px;
      background: rgba(239, 68, 68, 0.1);
      border-radius: 8px;
      margin-top: 8px;
    }
  `]
})
export class GeneratorComponent implements OnInit {
  sections = signal<SectionInfo[]>([]);
  units = signal<CurriculumUnitInfo[]>([]);
  selectedSection = '';
  selectedUnit: number | null = null;
  questionCount = 5;
  difficulty = 'medium';
  generating = signal(false);
  error = signal('');

  constructor(
    private exerciseService: ExerciseService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.exerciseService.getSections().subscribe(s => this.sections.set(s));
    this.exerciseService.getUnits().subscribe(u => this.units.set(u));

    this.route.queryParams.subscribe(params => {
      if (params['section']) {
        this.selectedSection = params['section'];
      }
    });
  }

  generate() {
    this.generating.set(true);
    this.error.set('');

    this.exerciseService.generate({
      sectionCode: this.selectedSection,
      curriculumUnitId: this.selectedUnit,
      questionCount: this.questionCount,
      difficulty: this.difficulty
    }).subscribe({
      next: (exercise) => {
        this.generating.set(false);
        this.router.navigate(['/exercise', exercise.id]);
      },
      error: (err) => {
        this.generating.set(false);
        this.error.set(err.error?.message || 'Unable to generate the exercise. Please try again.');
      }
    });
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }
}
