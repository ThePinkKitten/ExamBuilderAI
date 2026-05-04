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
    <div class="page-container focus-page">
      <div class="header-wrapper animate-fade-in">
        <button mat-icon-button (click)="goBack()" class="back-btn-aligned" matTooltip="Back to Dashboard">
          <mat-icon>arrow_back</mat-icon>
        </button>
        <div class="header-text-centered">
          <h1 class="gradient-text no-wrap">Generate Exercise</h1>
          <p>Choose settings and let AI generate an exercise for you</p>
        </div>
      </div>

      <div class="generator-focus-container animate-fade-in">
        <div class="generator-form glass-card">
          <!-- Exercise Type -->
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>
              Exercise type
              <mat-icon class="label-info-icon" 
                        matTooltip="Select the specific format of the exercise. AI quality is optimized for these types.">
                info
              </mat-icon>
            </mat-label>
            <mat-select [(ngModel)]="selectedSection" name="section" panelClass="custom-dropdown-panel">
              @for (s of sections(); track s.code) {
                <mat-option [value]="s.code">
                  {{ s.name }}
                </mat-option>
              }
            </mat-select>
            <mat-icon matPrefix>category</mat-icon>
          </mat-form-field>
          
          @if (selectedSection) {
            <div class="helper-text-container">
              <span class="helper-text">{{ getSelectedSectionDescription() }}</span>
            </div>
          }

          <!-- Curriculum Unit -->
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>
              Curriculum Unit
              <mat-icon class="label-info-icon" 
                        matTooltip="Aligns questions with specific vocabulary from your textbook unit.">
                info
              </mat-icon>
            </mat-label>
            <mat-select [(ngModel)]="selectedUnit" name="unit" placeholder="Select a unit (optional)" panelClass="custom-dropdown-panel">
              <mat-option [value]="null">-- All Units (Random) --</mat-option>
              @for (u of units(); track u.id) {
                <mat-option [value]="u.id">
                  Unit {{ u.unitNumber }}: {{ u.unitTitle }}
                </mat-option>
              }
            </mat-select>
            <mat-icon matPrefix>book</mat-icon>
          </mat-form-field>

          <!-- Question Count -->
          <div class="form-row">
            <mat-form-field appearance="outline" class="full-width">
            <mat-label>
              Number of questions
              <mat-icon class="label-info-icon" 
                        matTooltip="Recommended: 5-8 questions for best accuracy and natural phrasing.">
                info
              </mat-icon>
            </mat-label>
              <mat-select [(ngModel)]="selectedCountOption" (ngModelChange)="onCountOptionChange($event)" 
                          name="count" panelClass="custom-dropdown-panel">
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

          <button class="accent-btn generate-btn" (click)="generate()"
                  [disabled]="generating() || !selectedSection">
            @if (generating()) {
              <mat-spinner diameter="20"></mat-spinner>
              <span>AI is thinking (about 10-15s)...</span>
            } @else {
              <ng-container>
                <mat-icon>auto_awesome</mat-icon>
                <span>Generate Exercise</span>
              </ng-container>
            }
          </button>

          <div class="ai-status-centered">
            <div class="status-dot pulse"></div>
            <span>AI System Ready</span>
          </div>

          @if (error()) {
            <div class="error-message">
              <mat-icon>error</mat-icon>
              {{ error() }}
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .page-header {
      display: flex;
      align-items: center;
      gap: 16px;
      margin-bottom: 32px;
      padding-left: 48px;
      h1 { font-size: 24px; font-weight: 700; }
      p { color: var(--text-secondary); font-size: 14px; }
    }
    .focus-page {
      display: flex;
      flex-direction: column;
      align-items: center;
      min-height: calc(100vh - 64px);
      padding: 32px 24px !important;
      overflow-y: auto;
    }

    .header-wrapper {
      width: 100%;
      max-width: 600px;
      margin-bottom: 24px;
      position: relative;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
    }

    .back-btn-aligned {
      position: absolute;
      left: -8px;
      top: 0;
      color: var(--text-muted);
      
      @media (max-width: 800px) {
        position: static;
        align-self: flex-start;
        margin-bottom: 8px;
      }
    }

    .header-text-centered {
      text-align: center;
      
      h1 { 
        font-size: 28px; 
        margin-bottom: 4px; 
        line-height: 1.2;
        white-space: nowrap;
      }
      p { 
        font-size: 14px; 
        color: var(--text-secondary);
        margin: 0;
      }
    }
    .generator-focus-container {
      width: 100%;
      max-width: 600px;
    }
    .generator-form {
      display: flex;
      flex-direction: column;
      gap: 12px;
      padding: 32px 40px;
      box-shadow: 0 15px 40px rgba(0, 0, 0, 0.05);
    }
    .helper-text-container {
      margin-top: -12px;
      margin-bottom: 8px;
      padding-left: 2px;
    }
    .helper-text {
      font-size: 13px;
      color: #666;
      display: block;
      line-height: 1.4;
    }
    .label-info-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
      margin-left: 4px;
      vertical-align: middle;
      color: var(--text-muted);
      cursor: help;
      &:hover { color: var(--accent-primary); }
    }
    .ai-status-centered {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      font-size: 13px;
      font-weight: 600;
      color: var(--success);
      margin-top: 24px;
      .status-dot {
        width: 8px;
        height: 8px;
        background: var(--success);
        border-radius: 50%;
        &.pulse {
          box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.4);
          animation: statusPulse 2s infinite;
        }
      }
    }
    @keyframes statusPulse {
      0% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7); }
      70% { box-shadow: 0 0 0 10px rgba(16, 185, 129, 0); }
      100% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
    }
    .full-width { width: 100%; }
    .form-row {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }
    .generate-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 10px;
      height: 56px;
      font-size: 17px;
      margin-top: 10px;
      border-radius: 16px;
      z-index: 1;
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
      margin-top: 16px;
    }
  `]
})
export class GeneratorComponent implements OnInit {
  sections = signal<SectionInfo[]>([]);
  units = signal<CurriculumUnitInfo[]>([]);
  selectedSection = '';
  selectedUnit: number | null = null;
  selectedCountOption = 5;
  customCount = 5;
  questionCount = 5;
  generating = signal(false);
  error = signal('');
  
  getSelectedSectionDescription(): string {
    const section = this.sections().find(s => s.code === this.selectedSection);
    return section ? section.description : '';
  }

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
  }

  onCustomCountChange(value: number) {
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
