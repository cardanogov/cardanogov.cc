import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SkeletonComponent } from './skeleton.component';

@Component({
  selector: 'app-chart-skeleton',
  standalone: true,
  imports: [CommonModule, SkeletonComponent],
  template: `
    <div class="chart-skeleton">
      <div class="chart-header">
        <app-skeleton width="200px" height="20px" [rounded]="true"></app-skeleton>
      </div>
      <div class="chart-body">
        <div class="chart-grid">
          <div class="grid-lines">
            <div *ngFor="let i of [1,2,3,4,5]" class="grid-line">
              <app-skeleton width="40px" height="1px"></app-skeleton>
            </div>
          </div>
          <div class="chart-area">
            <app-skeleton width="100%" height="200px" [rounded]="true"></app-skeleton>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .chart-skeleton {
      background: white;
      border-radius: 0.5rem;
      padding: 1rem;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }
    .chart-header {
      margin-bottom: 1rem;
    }
    .chart-body {
      position: relative;
    }
    .chart-grid {
      position: relative;
      height: 200px;
    }
    .grid-lines {
      position: absolute;
      left: 0;
      top: 0;
      bottom: 0;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      padding: 1rem 0;
    }
    .grid-line {
      opacity: 0.5;
    }
    .chart-area {
      margin-left: 3rem;
    }
  `]
})
export class ChartSkeletonComponent {}
