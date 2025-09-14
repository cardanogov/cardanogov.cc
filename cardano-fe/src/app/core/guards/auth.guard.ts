import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const AuthGuard: CanActivateFn = () => {
  const isAuthenticated = inject(AuthService).isAuthenticated();

  if (!isAuthenticated) {
    localStorage.removeItem('token');
    return inject(Router).createUrlTree(['/auth/login']);
  }
  return true;
};
