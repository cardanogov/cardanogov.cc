import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnChanges,
  SimpleChanges,
  ChangeDetectionStrategy,
  OnDestroy,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import {
  NbCardModule,
  NbBadgeModule,
  NbInputModule,
  NbRadioModule,
  NbIconModule,
  NbTableModule,
  NbButtonModule,
  NbToastrModule,
  NbToastrService,
} from '@nebular/theme';
import { formatValue } from '../../../core/helper/format.helper';
import { RouterModule } from '@angular/router';
import { DateTimeAgoPipe } from '../../pipes/date-time-ago.pipe';
import { TimeAgoPipe } from "../../pipes/time-ago.pipe";
import { Subject } from 'rxjs';

export interface TableColumn {
  key: string;
  title: string;
  hasAlert?: boolean;
}

interface PaginationEvent {
  page: number;
  itemsPerPage: number;
}

@Component({
  selector: 'app-table',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NbCardModule,
    NbBadgeModule,
    NbInputModule,
    NbRadioModule,
    NbIconModule,
    NbTableModule,
    NbButtonModule,
    RouterModule,
    DateTimeAgoPipe,
    TimeAgoPipe
  ],
  templateUrl: './table.component.html',
  styleUrl: './table.component.scss',
  providers: [DatePipe],
  changeDetection: ChangeDetectionStrategy.Default,
})
export class TableComponent implements OnChanges, OnDestroy {
  @Input() title: string = '';
  @Input() subTitle: string = '';
  @Input() filters: { value: string; label: string }[] = [];
  @Input() columns: TableColumn[] = [];
  @Input() data: any[] = [];
  @Input() currentPage: number = 1;
  @Input() itemsPerPage: number = 10;
  @Input() totalItems: number = 0;
  @Input() showSearch?: boolean = true;
  @Input() showPagination?: boolean = true;
  @Input() showText?: boolean = false;
  @Input() text?: string = '';
  @Input() loading: boolean = false;
  @Input() fixedHeight: string = 'auto';
  @Input() showExpanded?: boolean = false;

  @Output() pageChange = new EventEmitter<PaginationEvent>();
  @Output() filterChange = new EventEmitter<Set<string>>();
  @Output() searchChange = new EventEmitter<string>();
  @Output() expand = new EventEmitter<void>();

  @Input() amountSort?: 'asc' | 'desc' | 'default';
  @Input() onAmountSort?: () => void;

  showFilters: boolean = false;
  selectedFilter: string | null = null;
  searchTerm: string = '';
  private destroy$ = new Subject<void>();

  constructor(private toastr: NbToastrService) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['currentPage']) {

    }
    // Validate and correct pagination inputs when they change
    if (
      changes['currentPage'] ||
      changes['itemsPerPage'] ||
      changes['totalItems']
    ) {
      this.validatePaginationInputs();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private validatePaginationInputs(): void {
    // Ensure positive values
    this.itemsPerPage = Math.max(1, this.itemsPerPage);
    this.totalItems = Math.max(0, this.totalItems);

    // Calculate valid page range
    const maxPage = this.getTotalPages();
    this.currentPage = Math.max(1, Math.min(this.currentPage, maxPage));
  }

  get pageInfo(): string {
    if (this.totalItems === 0) {
      return 'No items';
    }

    const start = (this.currentPage - 1) * this.itemsPerPage + 1;
    const end = Math.min(this.currentPage * this.itemsPerPage, this.totalItems);

    if (start > this.totalItems) {
      return `Page ${this.currentPage} (no items)`;
    }

    return `${start.toLocaleString()}-${end.toLocaleString()} of ${this.totalItems.toLocaleString()} items`;
  }

  get canGoBack(): boolean {
    return this.currentPage > 1;
  }

  get canGoForward(): boolean {
    return this.currentPage < this.getTotalPages();
  }

  get visibleData(): any[] {
    // Assuming this.data is already the data for the current page (fetched by the parent component).
    // No further client-side slicing is needed for pagination if the parent handles it.
    return this.data;
  }

  toggleFilters(): void {
    this.showFilters = !this.showFilters;
  }

  onFirstPage(): void {
    if (this.canGoBack) {
      this.emitPageChange(1);
    }
  }

  onPreviousPage(): void {
    if (this.canGoBack) {
      this.emitPageChange(this.currentPage - 1);
    }
  }

  onNextPage(): void {
    if (this.canGoForward) {
      this.emitPageChange(this.currentPage + 1);
    }
  }

  onLastPage(): void {
    if (this.canGoForward) {
      this.emitPageChange(this.getTotalPages());
    }
  }

  onFilterChange(value: string | null): void {
    if (value !== this.selectedFilter) {
      this.selectedFilter = value;
      this.emitFilterChange(value);
    }
  }

  onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchTerm = value;
    this.searchChange.emit(value);
    if (this.currentPage !== 1) {
      this.currentPage = 1;
      this.emitPageChange(this.currentPage);
    }
  }

  getVoteStatus(vote: string): string {
    switch (vote.toLowerCase()) {
      case 'yes':
        return 'success';
      case 'no':
        return 'danger';
      case 'abstain':
        return 'warning';
      default:
        return 'basic';
    }
  }

  getStatusBadge(status: string): string {
    switch (status.toLowerCase()) {
      case 'active':
        return 'success';
      case 'inactive':
        return 'danger';
      default:
        return 'basic';
    }
  }

  private getTotalPages(): number {
    return Math.max(1, Math.ceil(this.totalItems / this.itemsPerPage));
  }

  private emitPageChange(page: number): void {
    this.validatePaginationInputs();
    this.pageChange.emit({
      page,
      itemsPerPage: this.itemsPerPage,
    });
  }

  formatValue(value: any): string {
    if (value === null || value === undefined) {
      return '';
    }
    return formatValue(value);
  }

  truncateStakeAddress(address: string): string {
    if (!address) return '';
    if (address.length <= 20) return address;

    const start = address.substring(0, 10);
    const end = address.substring(address.length - 10);
    return `${start}...${end}`;
  }

  truncateName(name: string): string {
    if (!name) return '';
    if (name.length <= 30) return name;

    return name.substring(0, 30) + '...';
  }

  openUrl(url: string): void {
    if (url) {
      // Thêm giao thức nếu URL không bắt đầu bằng http:// hoặc https://
      const formattedUrl =
        url.startsWith('http://') || url.startsWith('https://')
          ? url
          : `https://${url}`;
      window.open(formattedUrl, '_blank');
    }
  }

    copyToClipboard(value: string) {
    if (!value) return;
    navigator.clipboard.writeText(value).then(
      () => {
        this.toastr.success('Copied to clipboard!', 'Success');
      },
      (err) => {
        this.toastr.danger('Failed to copy!', 'Error');
        console.error('Could not copy text: ', err);
      }
    );
  }

  private emitFilterChange(value: string | null): void {
    const filters = new Set<string>();
    if (value) {
      filters.add(value);
    }
    this.filterChange.emit(filters);
  }

  onExpand() {
    this.expand.emit();
  }
}
