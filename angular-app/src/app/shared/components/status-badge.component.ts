// ─────────────────────────────────────────────────────────────────────────────
// Status Badge Component — colored status labels
// ─────────────────────────────────────────────────────────────────────────────
import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'emm-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="badge" [ngClass]="statusClass">
      <span class="dot"></span>
      {{ status }}
    </span>
  `,
  styles: [`
    .badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 4px 12px;
      border-radius: 20px;
      font-size: 12px;
      font-weight: 600;
      text-transform: capitalize;
      letter-spacing: 0.3px;
    }

    .dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
    }

    .status-active, .status-healthy, .status-confirmed {
      background: rgba(0, 212, 170, 0.12);
      color: #00d4aa;
      .dot { background: #00d4aa; }
    }

    .status-inactive, .status-deactivated, .status-cancelled {
      background: rgba(255, 71, 87, 0.12);
      color: #ff4757;
      .dot { background: #ff4757; }
    }

    .status-pending, .status-processing {
      background: rgba(255, 193, 7, 0.12);
      color: #ffc107;
      .dot { background: #ffc107; }
    }

    .status-default {
      background: rgba(136, 136, 170, 0.12);
      color: #8888aa;
      .dot { background: #8888aa; }
    }
  `],
})
export class StatusBadgeComponent {
  @Input() status = '';

  get statusClass(): string {
    const normalized = this.status.toLowerCase();
    const knownStatuses = [
      'active', 'healthy', 'confirmed',
      'inactive', 'deactivated', 'cancelled',
      'pending', 'processing'
    ];
    if (knownStatuses.includes(normalized)) {
      return `status-${normalized}`;
    }
    return 'status-default';
  }
}
