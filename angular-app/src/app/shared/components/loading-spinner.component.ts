// ─────────────────────────────────────────────────────────────────────────────
// Loading Spinner Component
// ─────────────────────────────────────────────────────────────────────────────
import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'emm-loading',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="loading-container" [class.overlay]="overlay">
      <div class="spinner">
        <div class="ring"></div>
        <div class="ring ring-inner"></div>
      </div>
      <span class="loading-text" *ngIf="message">{{ message }}</span>
    </div>
  `,
  styles: [`
    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 16px;
      padding: 40px;

      &.overlay {
        position: absolute;
        inset: 0;
        background: rgba(15, 15, 35, 0.7);
        backdrop-filter: blur(4px);
        z-index: 10;
      }
    }

    .spinner {
      position: relative;
      width: 48px;
      height: 48px;
    }

    .ring {
      position: absolute;
      inset: 0;
      border: 3px solid transparent;
      border-top-color: var(--accent-primary);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    .ring-inner {
      inset: 6px;
      border-top-color: var(--accent-secondary);
      animation-duration: 0.6s;
      animation-direction: reverse;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .loading-text {
      font-size: 13px;
      color: var(--text-muted);
      letter-spacing: 0.5px;
    }
  `],
})
export class LoadingSpinnerComponent {
  @Input() message = '';
  @Input() overlay = false;
}
