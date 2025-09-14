import { Component, OnInit } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatCardModule } from '@angular/material/card';
import {
  CombineService,
  SearchApiResponse,
} from '../../../core/services/combine.service';
import { CommonModule } from '@angular/common';
import { Subject, debounceTime, switchMap } from 'rxjs';
import { NbSpinnerModule } from '@nebular/theme';
import { Router } from '@angular/router';
import { formatValue } from '../../../core/helper/format.helper';

@Component({
  selector: 'app-fullscreen-search',
  templateUrl: './fullscreen-search.component.html',
  styleUrls: ['./fullscreen-search.component.scss'],
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatToolbarModule,
    MatCardModule,
    NbSpinnerModule, // Added for spinner
    CommonModule,
  ],
})
export class FullScreenSearchComponent implements OnInit {
  isSearchOpen = false;
  searchResults: SearchApiResponse = {
    pools: [],
    dreps: [],
    charts: [],
    ccs: [],
    proposals: [],
  };
  searchQuery = '';
  loading = false; // Added to track loading state
  private searchTerms = new Subject<string>();

  constructor(private combineService: CombineService) {}

  ngOnInit() {
    this.searchTerms
      .pipe(
        switchMap((query) => {
          this.loading = true; // Set loading to true before the search
          return this.combineService.search(query);
        })
      )
      .subscribe({
        next: (data: SearchApiResponse) => {
          this.searchResults = data;
          this.loading = false; // Set loading to false when search completes
        },
        error: () => {
          this.searchResults = {
            pools: [],
            dreps: [],
            charts: [],
            ccs: [],
            proposals: [],
          };
          this.loading = false; // Set loading to false on error
        },
      });
  }

  openSearch() {
    this.isSearchOpen = true;
    setTimeout(() => {
      const input = document.querySelector(
        '.fullscreen-search-overlay input'
      ) as HTMLInputElement;
      if (input) {
        input.focus();
      }
    }, 100);
  }

  closeSearch() {
    this.isSearchOpen = false;
    this.searchResults = {
      pools: [],
      dreps: [],
      charts: [],
      ccs: [],
      proposals: [],
    };
    this.searchQuery = '';
    this.loading = false; // Reset loading state
    this.searchTerms.next(''); // Reset the search stream
  }

  onSearchInput(event: Event) {
    const target = event.target as HTMLInputElement;
    this.searchQuery = target?.value?.toLowerCase() || '';

    if (this.searchQuery && this.searchQuery.length > 0) {
      this.searchTerms.next(this.searchQuery); // Emit new search term
    } else {
      this.searchResults = {
        pools: [],
        dreps: [],
        charts: [],
        ccs: [],
        proposals: [],
      };
      this.loading = false; // Reset loading state when query is empty
    }
  }

  formatValue(value: number) {
    return formatValue(value);
  }

  onKeyDown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      this.closeSearch();
    }
  }
}
