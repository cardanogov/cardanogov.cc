// import { inject, Injectable } from '@angular/core';
// import { CanActivateFn, Router } from '@angular/router';
// import { AuthService } from '../services/auth.service';

// export const AdminGuard: CanActivateFn = (route, state) => {
//   const authService = inject(AuthService);
//   const router = new Router();
//   const user = authService.getUser();

//   if (user && user.role === 'admin') {
//     return true;
//   } else {
//     router.navigate(['/dashboard']);
//     return false;
//   }
// };
