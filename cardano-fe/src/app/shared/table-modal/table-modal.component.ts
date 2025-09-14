import { CommonModule } from '@angular/common';
import { Component, Inject, OnDestroy } from '@angular/core';
import { NbIconModule } from '@nebular/theme';
import { TableComponent } from '../components/table/table.component';
import { Subject } from 'rxjs';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-table-modal',
  imports: [CommonModule, NbIconModule, TableComponent],
  templateUrl: './table-modal.component.html',
  styleUrl: './table-modal.component.scss'
})
export class TableModalComponent implements OnDestroy {
  private destroy$ = new Subject<void>();

  currentPage = 1;
  itemsPerPage = 10;
  totalItems = 0;
  amountSort: 'asc' | 'desc' | 'default' = 'default';

  constructor(
    public dialogRef: MatDialogRef<TableModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private router: Router
  ) {
    this.totalItems = (data.data && Array.isArray(data.data)) ? data.data.length : 0;
    if (typeof data.itemsPerPage === 'number') {
      this.itemsPerPage = data.itemsPerPage;
    }
    if (typeof data.currentPage === 'number') {
      this.currentPage = data.currentPage;
    }
    // Close dialog on navigation
    this.router.events
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.dialogRef.close();
      });
  }

  onAmountSort = () => {
    if (this.amountSort === 'default') {
      this.amountSort = 'asc';
    } else if (this.amountSort === 'asc') {
      this.amountSort = 'desc';
    } else {
      this.amountSort = 'default';
    }
  };

  get pagedData() {
    if (!this.data.data || !Array.isArray(this.data.data)) return [];
    let data = [...this.data.data];
    // Only sort if column 'amount' exists
    if (this.data.columns && this.data.columns.some((col: any) => col.key === 'amount')) {
      if (this.amountSort === 'asc') {
        data.sort((a, b) => a.amount - b.amount);
      } else if (this.amountSort === 'desc') {
        data.sort((a, b) => b.amount - a.amount);
      } else {
        // Default: sort by time_ago if exists
        if (data.length && data[0].time_ago !== undefined) {
          data.sort((a, b) => a.time_ago - b.time_ago);
        }
      }
    }
    const start = (this.currentPage - 1) * this.itemsPerPage;
    const end = start + this.itemsPerPage;
    return data.slice(start, end);
  }

  onPageChange(event: { page: number; itemsPerPage: number }) {
    this.currentPage = event.page;
    this.itemsPerPage = event.itemsPerPage;
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
  

  close() {
    this.dialogRef.close();
  }
}
