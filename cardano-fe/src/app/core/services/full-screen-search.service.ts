import { Injectable } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class FullScreenSearchService {
  private openSearchSource = new Subject<void>();
  openSearch$ = this.openSearchSource.asObservable();

  private searchQuerySource = new BehaviorSubject<string>('');
  searchQuery$ = this.searchQuerySource.asObservable();

  openSearch() {
    this.openSearchSource.next();
  }

  updateSearchQuery(query: string) {
    this.searchQuerySource.next(query);
  }
}
