import { Component, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTabsModule],
  template: `
    <div class="login-page">
      <div class="login-container animate-fade-in">
        <div class="login-header">
          <mat-icon class="header-icon">school</mat-icon>
          <h1 class="gradient-text">ExamBuilder AI</h1>
          <p>Practice Grade 8 English with AI</p>
        </div>

        <mat-tab-group class="auth-tabs" [(selectedIndex)]="activeTab" animationDuration="300ms">
          <mat-tab label="Sign In">
            <form (ngSubmit)="onLogin()" class="auth-form">
              <mat-form-field appearance="outline" floatLabel="always">
                <mat-label>Username</mat-label>
                <mat-icon matPrefix class="field-icon">person</mat-icon>
                  <input matInput [(ngModel)]="loginUsername" name="loginUsername" required
                    placeholder="Enter username">
              </mat-form-field>

              <mat-form-field appearance="outline" floatLabel="always">
                <mat-label>Password</mat-label>
                <mat-icon matPrefix class="field-icon">lock</mat-icon>
                  <input matInput [type]="hidePassword() ? 'password' : 'text'"
                    [(ngModel)]="loginPassword" name="loginPassword" required
                    placeholder="Enter password">
                <button mat-icon-button matSuffix type="button"
                        (click)="hidePassword.set(!hidePassword())">
                  <mat-icon>{{ hidePassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
                </button>
              </mat-form-field>

              @if (error()) {
                <div class="error-message">{{ error() }}</div>
              }

              <button mat-flat-button class="accent-btn submit-btn" type="submit"
                      [disabled]="loading()">
                @if (loading()) {
                  <mat-spinner diameter="20"></mat-spinner>
                } @else {
                  Sign In
                }
              </button>
            </form>
          </mat-tab>

          <mat-tab label="Sign Up">
            <form (ngSubmit)="onRegister()" class="auth-form">
              <mat-form-field appearance="outline" floatLabel="always">
                <mat-label>Display name</mat-label>
                <mat-icon matPrefix class="field-icon">badge</mat-icon>
                  <input matInput [(ngModel)]="displayName" name="displayName" required
                    placeholder="Enter display name">
              </mat-form-field>

              <mat-form-field appearance="outline" floatLabel="always">
                <mat-label>Username</mat-label>
                <mat-icon matPrefix class="field-icon">person</mat-icon>
                  <input matInput [(ngModel)]="regUsername" name="regUsername" required
                    placeholder="Enter username">
              </mat-form-field>

              <mat-form-field appearance="outline" floatLabel="always">
                <mat-label>Password</mat-label>
                <mat-icon matPrefix class="field-icon">lock</mat-icon>
                  <input matInput [type]="hidePassword() ? 'password' : 'text'"
                    [(ngModel)]="regPassword" name="regPassword" required minlength="6"
                    placeholder="Enter password">
                <button mat-icon-button matSuffix type="button"
                        (click)="hidePassword.set(!hidePassword())">
                  <mat-icon>{{ hidePassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
                </button>
                <mat-hint align="start">At least 6 characters</mat-hint>
              </mat-form-field>

              @if (error()) {
                <div class="error-message">{{ error() }}</div>
              }

              <button mat-flat-button class="accent-btn submit-btn" type="submit"
                      [disabled]="loading()">
                @if (loading()) {
                  <mat-spinner diameter="20"></mat-spinner>
                } @else {
                  Sign Up
                }
              </button>
            </form>
          </mat-tab>
        </mat-tab-group>
      </div>

      <div class="bg-decoration">
        <div class="orb orb-1"></div>
        <div class="orb orb-2"></div>
        <div class="orb orb-3"></div>
      </div>
    </div>
  `,
  styles: [`
    .login-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background:
        radial-gradient(900px 520px at 90% -10%, rgba(251, 113, 133, 0.22), transparent 60%),
        radial-gradient(700px 420px at -10% 85%, rgba(225, 29, 72, 0.12), transparent 60%),
        transparent;
      position: relative;
      overflow: hidden;
    }

    .login-container {
      width: min(420px, 92vw);
      z-index: 1;
    }

    .login-header {
      text-align: center;
      margin-bottom: 32px;

      .header-icon {
        font-size: 56px;
        width: 72px;
        height: 72px;
        color: var(--accent-primary);
        margin-bottom: 12px;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        border-radius: 20px;
        background: rgba(251, 113, 133, 0.12);
        border: 1px solid rgba(225, 29, 72, 0.18);
        box-shadow: 0 10px 24px rgba(225, 29, 72, 0.12);
      }

      h1 {
        font-size: 28px;
        font-weight: 700;
        margin-bottom: 8px;
      }

      p {
        color: var(--text-secondary);
        font-size: 14px;
      }
    }

    .auth-tabs {
      background: rgba(255, 255, 255, 0.92);
      backdrop-filter: blur(16px);
      border: 1px solid rgba(225, 29, 72, 0.12);
      border-radius: var(--radius-lg);
      overflow: hidden;
      box-shadow: var(--shadow);
    }

    .auth-form {
      padding: 24px;
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .auth-form input::placeholder {
      color: var(--text-muted);
      opacity: 0.85;
    }

    .field-icon {
      color: var(--accent-secondary);
    }

    .submit-btn {
      margin-top: 8px;
      height: 48px;
      font-size: 16px;
      width: 100%;
    }

    .error-message {
      color: var(--danger);
      font-size: 13px;
      text-align: center;
      padding: 8px;
      background: rgba(220, 38, 38, 0.08);
      border-radius: 8px;
    }

    .auth-tabs ::ng-deep .mdc-tab__text-label {
      color: var(--text-secondary);
    }

    .auth-tabs ::ng-deep .mdc-tab--active .mdc-tab__text-label {
      color: var(--accent-primary);
    }

    .auth-tabs ::ng-deep .mdc-tab-indicator__content--underline {
      border-color: var(--accent-primary);
    }

    // Background decorations
    .bg-decoration {
      position: absolute;
      inset: 0;
      pointer-events: none;
    }

    .orb {
      position: absolute;
      border-radius: 50%;
      filter: blur(90px);
      opacity: 0.18;
    }

    .orb-1 {
      width: 420px;
      height: 420px;
      background: #fb7185;
      top: -120px;
      right: -120px;
      animation: pulse 8s ease-in-out infinite;
    }

    .orb-2 {
      width: 320px;
      height: 320px;
      background: #f9a8d4;
      bottom: -60px;
      left: -60px;
      animation: pulse 6s ease-in-out infinite 2s;
    }

    .orb-3 {
      width: 220px;
      height: 220px;
      background: #fecdd3;
      top: 55%;
      left: 55%;
      animation: pulse 10s ease-in-out infinite 4s;
    }
  `]
})
export class LoginComponent {
  activeTab = 0;
  loginUsername = '';
  loginPassword = '';
  regUsername = '';
  regPassword = '';
  displayName = '';
  hidePassword = signal(true);
  loading = signal(false);
  error = signal('');

  constructor(private auth: AuthService, private router: Router) {}

  onLogin() {
    this.loading.set(true);
    this.error.set('');
    this.auth.login({ username: this.loginUsername, password: this.loginPassword })
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.router.navigate(['/dashboard']);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err.error?.message || 'Sign in failed.');
        }
      });
  }

  onRegister() {
    this.loading.set(true);
    this.error.set('');
    this.auth.register({
      username: this.regUsername,
      password: this.regPassword,
      displayName: this.displayName
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading.set(false);
          this.error.set(err.error?.message || 'Sign up failed.');
      }
    });
  }
}
