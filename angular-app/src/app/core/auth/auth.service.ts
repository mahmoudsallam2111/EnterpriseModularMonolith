// ─────────────────────────────────────────────────────────────────────────────
// Auth Service — JWT token management for the Enterprise Modular Monolith
// ─────────────────────────────────────────────────────────────────────────────
import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

interface TokenPayload {
  sub: string;
  name: string;
  role: string;
  exp: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'emm_auth_token';
  private readonly tokenSignal = signal<string | null>(this.getStoredToken());

  readonly isAuthenticated = computed(() => {
    const token = this.tokenSignal();
    if (!token) return false;
    const payload = this.decodeToken(token);
    return payload !== null && payload.exp * 1000 > Date.now();
  });

  readonly currentUser = computed(() => {
    const token = this.tokenSignal();
    if (!token) return null;
    return this.decodeToken(token);
  });

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router
  ) {}

  /** Login using the dev auth endpoint */
  login(userId: string, role: string = 'Admin'): Promise<boolean> {
    return new Promise((resolve) => {
      this.http
        .post<{ token: string }>(`${environment.apiBaseUrl}/dev/auth/token`, {
          userId,
          userName: userId,
          role,
        })
        .subscribe({
          next: (response) => {
            this.setToken(response.token);
            resolve(true);
          },
          error: () => resolve(false),
        });
    });
  }

  /** Set token directly (for manual entry) */
  setToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
    this.tokenSignal.set(token);
  }

  /** Get the current token */
  getToken(): string | null {
    return this.tokenSignal();
  }

  /** Logout and redirect to login */
  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    this.tokenSignal.set(null);
    this.router.navigate(['/login']);
  }

  private getStoredToken(): string | null {
    if (typeof localStorage === 'undefined') return null;
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private decodeToken(token: string): TokenPayload | null {
    try {
      const payload = token.split('.')[1];
      const decoded = atob(payload);
      return JSON.parse(decoded);
    } catch {
      return null;
    }
  }
}
