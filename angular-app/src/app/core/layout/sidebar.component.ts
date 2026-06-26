// ─────────────────────────────────────────────────────────────────────────────
// Sidebar Component — navigation panel with glassmorphism design
// ─────────────────────────────────────────────────────────────────────────────
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

interface NavItem {
  label: string;
  route: string;
  icon: string;
}

@Component({
  selector: 'emm-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, MatIconModule, MatTooltipModule],
  template: `
    <aside class="sidebar" [class.collapsed]="collapsed">
      <!-- Logo -->
      <div class="logo-section">
        <div class="logo-icon">
          <span class="logo-letter">E</span>
        </div>
        <span class="logo-text" *ngIf="!collapsed">EMM</span>
      </div>

      <!-- Toggle -->
      <button class="toggle-btn" (click)="toggleCollapse.emit()">
        <mat-icon>{{ collapsed ? 'chevron_right' : 'chevron_left' }}</mat-icon>
      </button>

      <!-- Navigation -->
      <nav class="nav-list">
        <a
          *ngFor="let item of navItems"
          [routerLink]="item.route"
          routerLinkActive="active"
          [routerLinkActiveOptions]="{ exact: item.route === '/dashboard' }"
          class="nav-item"
          [matTooltip]="collapsed ? item.label : ''"
          matTooltipPosition="right"
        >
          <div class="nav-indicator"></div>
          <mat-icon class="nav-icon">{{ item.icon }}</mat-icon>
          <span class="nav-label" *ngIf="!collapsed">{{ item.label }}</span>
        </a>
      </nav>

      <!-- Footer -->
      <div class="sidebar-footer" *ngIf="!collapsed">
        <div class="version-badge">v1.0.0</div>
      </div>
    </aside>
  `,
  styles: [`
    .sidebar {
      position: fixed;
      top: 0;
      left: 0;
      height: 100vh;
      width: 260px;
      background: var(--bg-sidebar);
      backdrop-filter: blur(20px);
      border-right: 1px solid var(--border-glass);
      display: flex;
      flex-direction: column;
      z-index: 100;
      transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
      overflow: hidden;
    }

    .sidebar.collapsed {
      width: 72px;
    }

    .logo-section {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 24px 20px;
      border-bottom: 1px solid var(--border-glass);
    }

    .logo-icon {
      width: 36px;
      height: 36px;
      border-radius: 10px;
      background: var(--accent-gradient);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .logo-letter {
      color: white;
      font-weight: 700;
      font-size: 18px;
    }

    .logo-text {
      font-size: 20px;
      font-weight: 700;
      background: var(--accent-gradient);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
      white-space: nowrap;
    }

    .toggle-btn {
      position: absolute;
      top: 30px;
      right: -14px;
      width: 28px;
      height: 28px;
      border-radius: 50%;
      background: var(--bg-card);
      border: 1px solid var(--border-glass);
      color: var(--text-muted);
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      z-index: 10;
      transition: all 0.2s ease;

      mat-icon {
        font-size: 16px;
        width: 16px;
        height: 16px;
      }

      &:hover {
        color: var(--accent-primary);
        border-color: var(--accent-primary);
        box-shadow: var(--shadow-glow);
      }
    }

    .nav-list {
      flex: 1;
      padding: 16px 12px;
      display: flex;
      flex-direction: column;
      gap: 4px;
      overflow-y: auto;
    }

    .nav-item {
      position: relative;
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 14px;
      border-radius: 12px;
      color: var(--text-muted);
      text-decoration: none;
      transition: all 0.2s ease;
      cursor: pointer;
      overflow: hidden;

      &:hover {
        color: var(--text-primary);
        background: rgba(108, 99, 255, 0.08);
      }

      &.active {
        color: var(--accent-primary);
        background: rgba(108, 99, 255, 0.12);

        .nav-indicator {
          transform: scaleY(1);
        }

        .nav-icon {
          color: var(--accent-primary);
        }
      }
    }

    .nav-indicator {
      position: absolute;
      left: 0;
      top: 6px;
      bottom: 6px;
      width: 3px;
      border-radius: 0 3px 3px 0;
      background: var(--accent-gradient);
      transform: scaleY(0);
      transition: transform 0.2s ease;
    }

    .nav-icon {
      flex-shrink: 0;
      transition: color 0.2s ease;
    }

    .nav-label {
      font-size: 14px;
      font-weight: 500;
      white-space: nowrap;
    }

    .sidebar-footer {
      padding: 16px 20px;
      border-top: 1px solid var(--border-glass);
    }

    .version-badge {
      font-size: 11px;
      color: var(--text-muted);
      text-align: center;
      padding: 4px 8px;
      border-radius: 6px;
      background: rgba(255, 255, 255, 0.04);
    }
  `],
})
export class SidebarComponent {
  @Input() collapsed = false;
  @Output() toggleCollapse = new EventEmitter<void>();

  navItems: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard' },
    { label: 'Customers', route: '/customers', icon: 'people' },
    { label: 'Orders', route: '/orders', icon: 'shopping_cart' },
    { label: 'Inventories', route: '/inventories', icon: 'inventory_2' },
  ];
}
