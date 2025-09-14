import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private loadingSubject = new BehaviorSubject<boolean>(false);
  private spinnerTextSubject = new BehaviorSubject<string>('Loading...');

  loading$: Observable<boolean> = this.loadingSubject.asObservable();
  spinnerText$: Observable<string> = this.spinnerTextSubject.asObservable();

  show(text: string = 'Loading...') {
    this.loadingSubject.next(true);
    this.spinnerTextSubject.next(text);
  }

  hide() {
    this.loadingSubject.next(false);
    this.spinnerTextSubject.next('');
  }
}
