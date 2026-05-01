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
          <div class="count-group">
            <mat-form-field appearance="outline">
              <mat-label>Number of questions</mat-label>
              <mat-select [(ngModel)]="selectedCountOption" (ngModelChange)="onCountOptionChange($event)" name="count">
                <mat-option [value]="1">1 question</mat-option>
                <mat-option [value]="3">3 questions</mat-option>
                <mat-option [value]="5">5 questions</mat-option>
                <mat-option [value]="8">8 questions</mat-option>
                <mat-option [value]="10">10 questions</mat-option>
                <mat-option [value]="-1">✏️ Custom...</mat-option>
              </mat-select>
              <mat-icon matPrefix>format_list_numbered</mat-icon>
            </mat-form-field>

            @if (selectedCountOption === -1) {
              <mat-form-field appearance="outline" class="custom-count-field animate-fade-in">
                <mat-label>Custom (1–20)</mat-label>
                <input matInput type="number" [(ngModel)]="customCount" name="customCount"
                       min="1" max="20" (ngModelChange)="onCustomCountChange($event)">
                <mat-icon matPrefix>edit</mat-icon>
                <mat-hint>Enter 1 to 20</mat-hint>
              </mat-form-field>
            }
          </div>

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
      align-items: flex-start;

      mat-form-field { flex: 1; }
    }

    .count-group {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 0;
    }

    .custom-count-field {
      width: 100%;
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

  // Question count: -1 means "custom"
  selectedCountOption = 5;
  customCount = 5;
  questionCount = 5;  // The real value sent to API

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

  onCountOptionChange(value: number) {
    if (value !== -1) {
      this.questionCount = value;
    }
    // If custom, keep questionCount as last customCount
  }

  onCustomCountChange(value: number) {
    // Clamp between 1 and 20
    const clamped = Math.max(1, Math.min(20, value || 1));
    this.customCount = clamped;
    this.questionCount = clamped;
  }

  generate() {
    this.generating.set(true);
    this.error.set('');

    this.exerciseService.generate({
      sectionCode: this.selectedSection,
      curriculumUnitId: this.selectedUnit,
      questionCount: this.questionCount
    }).subscribe({
      next: (response: any) => {
        this.generating.set(false);
        if (response.code === 'BANK_UPDATING') {
          this.error.set(response.message);
        } else if (response.id) {
          this.router.navigate(['/exercise', response.id]);
        }
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
