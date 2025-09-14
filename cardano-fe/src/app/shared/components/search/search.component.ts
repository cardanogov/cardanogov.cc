import { Component, EventEmitter, Input, Output } from '@angular/core';
import { SearchService } from '../../../core/services/search.service';

@Component({
    selector: 'app-search',
    templateUrl: './search.component.html',
    styleUrls: ['./search.component.scss'],
    standalone: false
})
export class SearchComponent {
  searchText = '';

  constructor(private searchService: SearchService) {}

  onSearchClick() {
    this.searchService.updateSearch(this.searchText);
  }
}
