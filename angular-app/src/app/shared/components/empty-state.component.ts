// ─────────────────────────────────────────────────────────────────────────────
// Empty State Component — shown when no data is available
// ─────────────────────────────────────────────────────────────────────────────
import { Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'emm-empty-state',
  standalone: true,
  imports: [MatIconModule],
  template: `
    <div class="empty-state">
      <div class="empty-icon-wrapper">
        <mat-icon class="empty-icon">{{ icon }}</mat-icon>
      </div>
      <h3 class="empty-title">{{ title }}</h3>
      <p class="empty-message">{{ message }}</p>
    </div>
  `,
  styles: [`
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 60px 20px;
      text-align: center;
    }

    .empty-icon-wrapper {
      width: 80px;
      height: 80px;
      border-radius: 50%;
      background: rgba(108, 99, 255, 0.08);
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 20px;
    }

    .empty-icon {
      font-size: 36px;
      width: 36px;
      height: 36px;
      color: var(--accent-primary);
      opacity: 0.6;
    }

    .empty-title {
      font-size: 18px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 8px 0;
    }

    .empty-message {
      font-size: 14px;
      color: var(--text-muted);
      margin: 0;
      max-width: 320px;
    }
  `],
})
export class EmptyStateComponent {
  @Input() icon = 'inbox';
  @Input() title = 'No data';
  @Input() message = 'There are no items to display.';
}
