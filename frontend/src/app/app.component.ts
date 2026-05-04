import { Component, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatSidenavModule, MatToolbarModule, MatIconModule, MatButtonModule, MatListModule, MatTooltipModule],
  template: `
    @if (auth.isLoggedIn()) {
      <div class="app-layout" [class.collapsed]="isCollapsed()">
        <nav class="sidebar">
          <div class="sidebar-header">
            <div class="logo">
              <mat-icon class="logo-icon">school</mat-icon>
              @if (!isCollapsed()) {
                <span class="logo-text gradient-text">ExamBuilder AI</span>
              }
            </div>
          </div>

          <div class="nav-links">
            <a routerLink="/dashboard" routerLinkActive="active" class="nav-item"
               [matTooltip]="isCollapsed() ? 'Dashboard' : ''" matTooltipPosition="right">
              <mat-icon>dashboard</mat-icon>
              @if (!isCollapsed()) { <span>Dashboard</span> }
            </a>
            <a routerLink="/exercise/generate" routerLinkActive="active" class="nav-item"
               [matTooltip]="isCollapsed() ? 'Generate Exercise' : ''" matTooltipPosition="right">
              <mat-icon>auto_awesome</mat-icon>
              @if (!isCollapsed()) { <span>Generate Exercise</span> }
            </a>
            <a routerLink="/progress" routerLinkActive="active" class="nav-item"
               [matTooltip]="isCollapsed() ? 'Progress' : ''" matTooltipPosition="right">
              <mat-icon>trending_up</mat-icon>
              @if (!isCollapsed()) { <span>Progress</span> }
            </a>
            <a routerLink="/history" routerLinkActive="active" class="nav-item"
               [matTooltip]="isCollapsed() ? 'History' : ''" matTooltipPosition="right">
              <mat-icon>history</mat-icon>
              @if (!isCollapsed()) { <span>History</span> }
            </a>
            <a routerLink="/retake" routerLinkActive="active" class="nav-item"
               [matTooltip]="isCollapsed() ? 'Mistake Bank' : ''" matTooltipPosition="right">
              <mat-icon>build_circle</mat-icon>
              @if (!isCollapsed()) { <span>Mistake Bank</span> }
            </a>
          </div>

          <div class="sidebar-footer">
            <div class="user-action-row" [class.centered]="isCollapsed()" (click)="auth.logout()" 
                 [matTooltip]="isCollapsed() ? 'Logout (' + auth.currentUser()?.displayName + ')' : 'Logout'" matTooltipPosition="right">
              <div class="user-info">
                <mat-icon class="user-avatar">account_circle</mat-icon>
                @if (!isCollapsed()) {
                  <span class="username">{{ auth.currentUser()?.displayName }}</span>
                }
              </div>
              
              @if (!isCollapsed()) {
                <mat-icon class="logout-icon">logout</mat-icon>
              }
            </div>
          </div>
        </nav>

        <main class="main-content">
          <button mat-icon-button (click)="toggleSidebar()" class="toggle-btn-float" 
                  [matTooltip]="isCollapsed() ? 'Expand Sidebar' : 'Collapse Sidebar'">
            <mat-icon>{{ isCollapsed() ? 'menu_open' : 'menu' }}</mat-icon>
          </button>
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
      transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    }

    .sidebar {
      width: 260px;
      background: var(--bg-secondary);
      border-right: 1px solid var(--border);
      display: flex;
      flex-direction: column;
      padding: 20px 0;
      flex-shrink: 0;
      transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
      position: relative;
    }

    .collapsed .sidebar {
      width: 80px;
    }

    .sidebar-header {
      padding: 0 16px 24px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      border-bottom: 1px solid var(--border);
    }

    .collapsed .sidebar-header {
      justify-content: center;
      padding: 0 0 24px;
    }

    .logo {
      display: flex;
      align-items: center;
      gap: 12px;
      overflow: hidden;
    }

    .logo-icon {
      font-size: 32px;
      width: 32px;
      height: 32px;
      color: var(--accent-primary);
      flex-shrink: 0;
    }

    .logo-text {
      font-size: 18px;
      font-weight: 700;
      white-space: nowrap;
    }

    .main-content {
      flex: 1;
      overflow-y: auto;
      background: var(--bg-primary);
      position: relative;
    }

    .toggle-btn-float {
      position: absolute;
      top: 20px;
      left: 12px;
      z-index: 100;
      color: var(--text-muted);
      width: 40px;
      height: 40px;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s ease;
      background: transparent !important; // Ensure no background

      mat-icon { font-size: 24px; width: 24px; height: 24px; }

      &:hover {
        color: var(--text-primary);
        background: #f1f5f9 !important; // Subtle gray hover
      }
    }

    .nav-links {
      flex: 1;
      padding: 16px; // Restored healthy horizontal padding (px-4)
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .nav-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 16px;
      border-radius: 12px;
      color: var(--text-secondary);
      text-decoration: none;
      font-size: 14px;
      font-weight: 500;
      transition: all 0.2s ease;
      white-space: nowrap;
      overflow: hidden;

      mat-icon {
        font-size: 22px;
        width: 22px;
        height: 22px;
        color: var(--text-muted);
        transition: color 0.2s;
        flex-shrink: 0;
      }

      &:hover {
        background: rgba(244, 63, 94, 0.06);
        color: var(--accent-primary);
        mat-icon { color: var(--accent-primary); }
      }

      &.active {
        background: var(--accent-gradient);
        color: white;
        box-shadow: none !important;
        mat-icon { color: white; }
      }
    }

    .collapsed .nav-item {
      justify-content: center;
      padding: 12px;
      margin: 0; 
      gap: 0;
    }

    .sidebar-footer {
      padding: 16px; 
      border-top: 1px solid var(--border);
    }

    .collapsed .sidebar-footer {
      padding: 16px 8px;
    }

    .user-action-row {
      display: flex;
      align-items: center;
      padding: 12px 16px;
      border-radius: 12px;
      transition: all 0.2s ease;
      cursor: pointer;
      width: 100%;
      color: var(--text-secondary);

      &:hover {
        background: rgba(244, 63, 94, 0.05);
        color: var(--accent-primary);
        .logout-icon { color: var(--danger); }
      }

      &.centered {
        justify-content: center;
        padding: 12px 0;
      }
    }

    .user-info {
      display: flex;
      align-items: center;
      gap: 12px;
      overflow: hidden;
      white-space: nowrap;

      .user-avatar {
        color: var(--accent-primary);
        font-size: 24px;
        width: 24px;
        height: 24px;
        display: flex;
        align-items: center;
        justify-content: center;
      }

      .username {
        font-size: 14px;
        font-weight: 600;
      }
    }

    .logout-icon {
      margin-left: auto; // Push to right
      font-size: 20px;
      width: 20px;
      height: 20px;
      color: var(--text-muted);
      transition: all 0.2s ease;
      display: flex;
      align-items: center;
      justify-content: center;

      &:hover {
        color: var(--danger);
        transform: translateX(2px);
      }
    }

    .main-content {
      flex: 1;
      overflow-y: auto;
      background: var(--bg-primary);
      padding: 0;
    }
  `]
})
export class AppComponent {
  isCollapsed = signal(false);

  constructor(public auth: AuthService) {
    // Auto-collapse on small screens
    if (window.innerWidth < 1024) {
      this.isCollapsed.set(true);
    }
  }

  toggleSidebar() {
    this.isCollapsed.update(v => !v);
  }
}
