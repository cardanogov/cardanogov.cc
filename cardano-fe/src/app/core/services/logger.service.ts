import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoggerService {
  log(message: string, data?: any) {
  }

  error(message: string, data?: any) {
    console.error(`[ERROR]: ${message}`, data || '');
  }

  warn(message: string, data?: any) {
  }
}
