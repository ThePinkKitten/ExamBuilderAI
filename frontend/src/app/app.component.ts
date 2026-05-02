import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatSidenavModule, MatToolbarModule, MatIconModule, MatButtonModule, MatListModule],
  template: `
    @if (auth.isLoggedIn()) {
      <div class="app-layout">
        <nav class="sidebar">
          <div class="sidebar-header">
            <div class="logo">
              <mat-icon class="logo-icon">school</mat-icon>
              <span class="logo-text gradient-text">ExamBuilder AI</span>
            </div>
          </div>

          <div class="nav-links">
            <a routerLink="/dashboard" routerLinkActive="active" class="nav-item">
              <mat-icon>dashboard</mat-icon>
              <span>Dashboard</span>
            </a>
            <a routerLink="/exercise/generate" routerLinkActive="active" class="nav-item">
              <mat-icon>auto_awesome</mat-icon>
              <span>Generate Exercise</span>
            </a>
            <a routerLink="/progress" routerLinkActive="active" class="nav-item">
              <mat-icon>trending_up</mat-icon>
              <span>Progress</span>
            </a>
            <a routerLink="/history" routerLinkActive="active" class="nav-item">
              <mat-icon>history</mat-icon>
              <span>History</span>
            </a>
            <a routerLink="/retake" routerLinkActive="active" class="nav-item">
              <mat-icon>build_circle</mat-icon>
              <span>Mistake Bank</span>
            </a>
          </div>

          <div class="sidebar-footer">
            <div class="user-info">
              <mat-icon>account_circle</mat-icon>
              <span>{{ auth.currentUser()?.displayName }}</span>
            </div>
            <button mat-icon-button (click)="auth.logout()" class="logout-btn">
              <mat-icon>logout</mat-icon>
            </button>
          </div>
        </nav>

        <main class="main-content">
          <router-outlet />
        </main>
      </div>
    } @else {
      <router-outlet />
    }
  `,
  styles: [`
    .app-layout {
      display: flex;
      height: 100vh;
    }

    .sidebar {
      width: 260px;
      background: var(--bg-secondary);
      border-right: 1px solid var(--border);
      display: flex;
      flex-direction: column;
      padding: 20px 0;
      flex-shrink: 0;
    }

    .sidebar-header {
      padding: 0 20px 24px;
      border-bottom: 1px solid var(--border);
    }

    .logo {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .logo-icon {
      font-size: 32px;
      width: 32px;
      height: 32px;
      color: var(--accent-secondary);
    }

    .logo-text {
      font-size: 20px;
      font-weight: 700;
    }

    .nav-links {
      flex: 1;
      padding: 16px 12px;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .nav-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 16px;
      border-radius: 10px;
      color: var(--text-secondary);
      text-decoration: none;
      font-size: 14px;
      font-weight: 500;
      transition: all 0.2s ease;

      mat-icon {
        font-size: 20px;
        width: 20px;
        height: 20px;
        color: var(--accent-secondary);
      }

      &:hover {
        background: rgba(225, 29, 72, 0.08);
        color: var(--accent-primary);

        mat-icon { color: var(--accent-primary); }
      }

      &.active {
        background: rgba(225, 29, 72, 0.14);
        color: var(--accent-primary);

        mat-icon { color: var(--accent-primary); }
      }
    }

    .sidebar-footer {
      padding: 16px 20px;
      border-top: 1px solid var(--border);
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .user-info {
      display: flex;
      align-items: center;
      gap: 8px;
      color: var(--text-secondary);
      font-size: 13px;

      mat-icon {
        color: var(--accent-secondary);
      }
    }

    .logout-btn {
      color: var(--accent-secondary);
      &:hover { color: var(--danger); }
    }

    .main-content {
      flex: 1;
      overflow-y: auto;
      background: var(--bg-primary);
    }
  `]
})
export class AppComponent {
  constructor(public auth: AuthService) {}
}
