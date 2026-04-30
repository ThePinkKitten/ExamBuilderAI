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
          <p>Ôn tập Tiếng Anh lớp 8 với AI</p>
        </div>

        <mat-tab-group class="auth-tabs" [(selectedIndex)]="activeTab" animationDuration="300ms">
          <mat-tab label="Đăng nhập">
            <form (ngSubmit)="onLogin()" class="auth-form">
              <mat-form-field appearance="outline">
                <mat-label>Username</mat-label>
                <input matInput [(ngModel)]="loginUsername" name="loginUsername" required>
                <mat-icon matPrefix>person</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Password</mat-label>
                <input matInput [type]="hidePassword() ? 'password' : 'text'"
                       [(ngModel)]="loginPassword" name="loginPassword" required>
                <mat-icon matPrefix>lock</mat-icon>
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
                  Đăng nhập
                }
              </button>
            </form>
          </mat-tab>

          <mat-tab label="Đăng ký">
            <form (ngSubmit)="onRegister()" class="auth-form">
              <mat-form-field appearance="outline">
                <mat-label>Tên hiển thị</mat-label>
                <input matInput [(ngModel)]="displayName" name="displayName" required>
                <mat-icon matPrefix>badge</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Username</mat-label>
                <input matInput [(ngModel)]="regUsername" name="regUsername" required>
                <mat-icon matPrefix>person</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Password (tối thiểu 6 ký tự)</mat-label>
                <input matInput [type]="hidePassword() ? 'password' : 'text'"
                       [(ngModel)]="regPassword" name="regPassword" required minlength="6">
                <mat-icon matPrefix>lock</mat-icon>
              </mat-form-field>

              @if (error()) {
                <div class="error-message">{{ error() }}</div>
              }

              <button mat-flat-button class="accent-btn submit-btn" type="submit"
                      [disabled]="loading()">
                @if (loading()) {
                  <mat-spinner diameter="20"></mat-spinner>
                } @else {
                  Đăng ký
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
      background: var(--bg-primary);
      position: relative;
      overflow: hidden;
    }

    .login-container {
      width: 420px;
      z-index: 1;
    }

    .login-header {
      text-align: center;
      margin-bottom: 32px;

      .header-icon {
        font-size: 56px;
        width: 56px;
        height: 56px;
        color: #7c3aed;
        margin-bottom: 12px;
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
      background: rgba(22, 33, 62, 0.8);
      backdrop-filter: blur(16px);
      border: 1px solid var(--border);
      border-radius: var(--radius-lg);
      overflow: hidden;
    }

    .auth-form {
      padding: 24px;
      display: flex;
      flex-direction: column;
      gap: 8px;
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
      background: rgba(239, 68, 68, 0.1);
      border-radius: 8px;
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
      filter: blur(80px);
      opacity: 0.15;
    }

    .orb-1 {
      width: 400px;
      height: 400px;
      background: #7c3aed;
      top: -100px;
      right: -100px;
      animation: pulse 8s ease-in-out infinite;
    }

    .orb-2 {
      width: 300px;
      height: 300px;
      background: #06b6d4;
      bottom: -50px;
      left: -50px;
      animation: pulse 6s ease-in-out infinite 2s;
    }

    .orb-3 {
      width: 200px;
      height: 200px;
      background: #8b5cf6;
      top: 50%;
      left: 50%;
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
          this.error.set(err.error?.message || 'Đăng nhập thất bại');
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
        this.error.set(err.error?.message || 'Đăng ký thất bại');
      }
    });
  }
}
