// ─────────────────────────────────────────────────────────────────────────────
// Shell Component — main app layout with sidebar + content area
// ─────────────────────────────────────────────────────────────────────────────
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './sidebar.component';
import { HeaderComponent } from './header.component';

@Component({
  selector: 'emm-shell',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, HeaderComponent],
  template: `
    <div class="shell" [class.sidebar-collapsed]="sidebarCollapsed">
      <emm-sidebar
        [collapsed]="sidebarCollapsed"
        (toggleCollapse)="sidebarCollapsed = !sidebarCollapsed"
      />
      <div class="main-area">
        <emm-header (toggleSidebar)="sidebarCollapsed = !sidebarCollapsed" />
        <main class="content" @routeAnimation>
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: [`
    .shell {
      display: flex;
      min-height: 100vh;
      background: var(--bg-primary);
    }

    .main-area {
      flex: 1;
      display: flex;
      flex-direction: column;
      margin-left: 260px;
      transition: margin-left 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    }

    .sidebar-collapsed .main-area {
      margin-left: 72px;
    }

    .content {
      flex: 1;
      padding: 28px 32px;
      overflow-y: auto;
    }

    @media (max-width: 768px) {
      .main-area {
        margin-left: 0;
      }
    }
  `],
})
export class ShellComponent {
  sidebarCollapsed = false;
}
