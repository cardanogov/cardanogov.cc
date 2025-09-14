import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
@Component({
  selector: 'app-table-skeleton',
  imports: [CommonModule],
  template: `
    <div class="table-skeleton mb-4">
      <div class="skeleton-header"></div>
      <div class="skeleton-row" *ngFor="let row of rows">
        <div class="skeleton-cell" *ngFor="let col of cols"></div>
      </div>
    </div>
  `,
  styleUrls: ['./table-skeleton.component.scss']
})
export class TableSkeletonComponent {
  rows = Array(4);
  cols = Array(5);
} 