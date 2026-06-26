// ─────────────────────────────────────────────────────────────────────────────
// Notification Service — snackbar-based notifications
// ─────────────────────────────────────────────────────────────────────────────
import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  constructor(private readonly snackBar: MatSnackBar) {}

  success(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 4000,
      panelClass: ['snack-success'],
      horizontalPosition: 'end',
      verticalPosition: 'top',
    });
  }

  error(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 6000,
      panelClass: ['snack-error'],
      horizontalPosition: 'end',
      verticalPosition: 'top',
    });
  }

  info(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 4000,
      panelClass: ['snack-info'],
      horizontalPosition: 'end',
      verticalPosition: 'top',
    });
  }
}
