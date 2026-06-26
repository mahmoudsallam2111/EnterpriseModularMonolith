// ─────────────────────────────────────────────────────────────────────────────
// Header Component — top bar with user info and health status
// ─────────────────────────────────────────────────────────────────────────────
import { Component, Output, EventEmitter, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { AuthService } from '../auth/auth.service';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'emm-header',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatMenuModule],
  template: `
    <header class="header">
      <div class="header-left">
        <button mat-icon-button class="menu-toggle" (click)="toggleSidebar.emit()">
          <mat-icon>menu</mat-icon>
        </button>
        <div class="breadcrumb">
          <span class="app-title">Enterprise Modular Monolith</span>
        </div>
      </div>

      <div class="header-right">
        <!-- Health Status -->
        <div class="health-indicator" [class.healthy]="healthStatus() === 'Healthy'"
             [class.unhealthy]="healthStatus() === 'Unhealthy'">
          <div class="health-dot"></div>
          <span class="health-text">{{ healthStatus() || 'Checking...' }}</span>
        </div>


        <!-- User Menu -->
        <button class="user-btn" [matMenuTriggerFor]="userMenu">
          <div class="user-avatar">
            <span>{{ userInitial() }}</span>
          </div>
          <span class="user-name">{{ userName() }}</span>
          <mat-icon class="dropdown-icon">expand_more</mat-icon>
        </button>

        <mat-menu #userMenu="matMenu" class="user-menu-panel">
          <div class="menu-header" mat-menu-item disabled>
            <div class="menu-user-info">
              <strong>{{ userName() }}</strong>
              <span class="menu-role">{{ userRole() }}</span>
            </div>
          </div>
          <button mat-menu-item (click)="logout()">
            <mat-icon>logout</mat-icon>
            <span>Sign Out</span>
          </button>
        </mat-menu>
      </div>
    </header>
  `,
  styles: [`
    .header {
      height: 64px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 28px;
      background: var(--bg-secondary);
      border-bottom: 1px solid var(--border-glass);
      backdrop-filter: blur(12px);
    }

    .header-left {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .menu-toggle {
      color: var(--text-muted);
      display: none;

      @media (max-width: 768px) {
        display: flex;
      }
    }

    .app-title {
      font-size: 14px;
      font-weight: 500;
      color: var(--text-muted);
      letter-spacing: 0.5px;
    }

    .header-right {
      display: flex;
      align-items: center;
      gap: 20px;
    }

    .health-indicator {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 6px 14px;
      border-radius: 20px;
      background: rgba(255, 255, 255, 0.04);
      border: 1px solid var(--border-glass);
      transition: all 0.3s ease;

      &.healthy {
        .health-dot {
          background: var(--accent-secondary);
          box-shadow: 0 0 8px rgba(0, 212, 170, 0.5);
        }
        .health-text { color: var(--accent-secondary); }
      }

      &.unhealthy {
        .health-dot {
          background: #ff4757;
          box-shadow: 0 0 8px rgba(255, 71, 87, 0.5);
        }
        .health-text { color: #ff4757; }
      }
    }

    .health-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: var(--text-muted);
      animation: pulse 2s infinite;
    }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.5; }
    }

    .health-text {
      font-size: 12px;
      font-weight: 600;
      color: var(--text-muted);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .user-btn {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 6px 12px;
      border: 1px solid var(--border-glass);
      border-radius: 12px;
      background: transparent;
      color: var(--text-primary);
      cursor: pointer;
      transition: all 0.2s ease;

      &:hover {
        background: rgba(108, 99, 255, 0.08);
        border-color: rgba(108, 99, 255, 0.3);
      }
    }

    .user-avatar {
      width: 32px;
      height: 32px;
      border-radius: 8px;
      background: var(--accent-gradient);
      display: flex;
      align-items: center;
      justify-content: center;

      span {
        color: white;
        font-weight: 700;
        font-size: 14px;
      }
    }

    .user-name {
      font-size: 13px;
      font-weight: 500;
    }

    .dropdown-icon {
      font-size: 18px;
      color: var(--text-muted);
    }

    .menu-header {
      pointer-events: none;
    }

    .menu-user-info {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .menu-role {
      font-size: 12px;
      color: var(--text-muted);
    }
  `],
})
export class HeaderComponent implements OnInit {
  @Output() toggleSidebar = new EventEmitter<void>();

  healthStatus = signal<string | null>(null);
  userName = signal<string>('User');
  userRole = signal<string>('Admin');
  userInitial = signal<string>('U');
  isRegistering = signal<boolean>(false);

  constructor(
    private readonly authService: AuthService,
    private readonly http: HttpClient,
  ) {}

  ngOnInit(): void {
    this.checkHealth();
    this.loadUserInfo();
  }

  logout(): void {
    this.authService.logout();
  }

  private checkHealth(): void {
    this.http.get<{status: string}>('/health').subscribe({
      next: (result) => this.healthStatus.set(result.status),
      error: () => this.healthStatus.set('Unhealthy'),
    });
  }

  private loadUserInfo(): void {
    const user = this.authService.currentUser();
    if (user) {
      this.userName.set(user.name || user.sub || 'User');
      this.userRole.set(user.role || 'User');
      this.userInitial.set((user.name || user.sub || 'U').charAt(0).toUpperCase());
    }
  }

}
