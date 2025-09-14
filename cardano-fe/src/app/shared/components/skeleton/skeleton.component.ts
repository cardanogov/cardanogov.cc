import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-skeleton',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="skeleton-wrapper" [class.animate-pulse]="animate">
      <div class="skeleton" [style.width]="width" [style.height]="height" [class.rounded]="rounded"></div>
    </div>
  `,
  styles: [`
    .skeleton-wrapper {
      background: #f3f4f6;
      overflow: hidden;
      height: 100%; /* Ensure wrapper takes full height of parent */
      display: flex; /* Make wrapper a flex container */
      align-items: stretch; /* Stretch child to fill height */
    }
    .skeleton {
      background: linear-gradient(90deg, #f3f4f6 0%, #e5e7eb 50%, #f3f4f6 100%);
      background-size: 200% 100%;
      /* Remove flex: 1 to prevent overriding width */
      min-width: 0; /* Prevent flex item from growing beyond its content */
    }
    .animate-pulse .skeleton {
      animation: shimmer 2s linear infinite;
    }
    .rounded {
      border-radius: 0.375rem;
    }
    @keyframes shimmer {
      0% {
        background-position: -200% 0;
      }
      100% {
        background-position: 200% 0;
      }
    }
  `]
})
export class SkeletonComponent {
  @Input() width: string = '100%';
  @Input() height: string = '100%';
  @Input() rounded: boolean = false;
  @Input() animate: boolean = true;
}

export * from './table-skeleton.component';