import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SkeletonComponent } from './skeleton.component';

@Component({
  selector: 'app-card-skeleton',
  standalone: true,
  imports: [CommonModule, SkeletonComponent],
  template: `
    <div class="card-skeleton" [style.height]="height">
      <div class="skeleton-content">
        <!-- Title -->
        <div class="skeleton-item title">
          <app-skeleton width="40%" height="100%" [rounded]="true"></app-skeleton>
        </div>

        <!-- Main value -->
        <div class="skeleton-item main">
          <app-skeleton width="100%" height="100%" [rounded]="true"></app-skeleton>
        </div>

        <!-- Additional info -->
        <div class="skeleton-item info">
          <app-skeleton width="60%" height="100%" [rounded]="true"></app-skeleton>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .card-skeleton {
      background: white;
      border-radius: 0.5rem;
      padding: 1.5rem;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
      box-sizing: border-box;
      height: 100%;
      display: flex;
      flex-direction: column;
      margin-bottom: 4px;
    }

    .skeleton-content {
      flex: 1;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      gap: 0.5rem;
    }

    .skeleton-item {
      flex: 1;
      display: flex;
      align-items: stretch; /* Stretch child vertically */
      justify-content: flex-start; /* Align child to start horizontally */

      app-skeleton {
        height: 100%;
        width: 100%;
      }
    }

    .title {
      flex: 1;
    }

    .main {
      flex: 2;
    }

    .info {
      flex: 1;
    }
  `]
})
export class CardSkeletonComponent {
  @Input() height: string = '100%';
}